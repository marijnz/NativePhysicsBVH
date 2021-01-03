using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace NativeBVH {
    public struct Leaf {
        public Collider collider;
        public RigidTransform transform;
        public uint layer;
    }
    
    public struct Node {
        public AABB3D box;
        public int parentIndex;
        public bool isLeaf;
        
        // Leaf only
        public Leaf leaf;
        
        // Internal only
        public SimpleFourTransposedAabbs grandchildrenAabbs;
        public int child1;
        public int child2;
    }

    /// <summary>
    /// Implemented per https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf. WIP.
    ///
    /// Optimizations left:
    /// - Minimize size of Node
    /// </summary>
    public unsafe partial struct NativeBVHTree : IDisposable {
        public struct Configuration {
            public float BoundsExpansion; // To support some wiggle room for moving objects
        }
        
        public const int InvalidNode = 0;
        public const int TreeTraversalStackSize = 256;
        
        [NativeDisableUnsafePtrRestriction]
        private UnsafeNodesList* nodesList;

        /// NOTE: This should only be used for reading. This can't be forced through an interface as it would make it managed.
        internal UnsafeNodesList nodes => *nodesList;
        
        private NativeArray<int> rootIndex;
        
        private Configuration config;

        public NativeBVHTree(int initialCapacity = 64, Allocator allocator = Allocator.Temp, Configuration config = default) : this() {
            nodesList = UnsafeNodesList.Create(initialCapacity, allocator, NativeArrayOptions.ClearMemory);

            rootIndex = new NativeArray<int>(1, allocator);

            // Create invalid node (at index 0)
            AllocInternalNode();

            this.config = config;
        }

        public int InsertLeaf(Collider collider, uint layer = 0xffffffff) {
            return InsertLeaf(new Leaf {collider = collider, layer = layer, transform = RigidTransform.identity});
        }
        
        public int InsertLeaf(Collider collider, RigidTransform transform, uint layer = 0xffffffff) {
            return InsertLeaf(new Leaf {collider = collider, layer = layer, transform = transform});
        }

        public int InsertLeaf(Leaf entry) {
            var leafIndex = AllocLeafNode(ref entry);
            var bounds = nodes[leafIndex]->box;
            
            if (rootIndex[0] == InvalidNode) {
                rootIndex[0] = leafIndex;
                return leafIndex;
            }
            
            // Stage 1: find the best sibling for the new leaf
            float bestCost = float.MaxValue;
            int bestIndex = -1;

            var heap = stackalloc UnsafeMinHeap.HeapItem[TreeTraversalStackSize];
            var insertionHeap = new UnsafeMinHeap.MinHeap {
                count = 0,
                heap = heap,
            };
            
            UnsafeMinHeap.Push(ref insertionHeap, new UnsafeMinHeap.HeapItem {Id = rootIndex[0], Cost = 0});
            
            while (insertionHeap.count != 0) {
                var heapItem = UnsafeMinHeap.Pop(ref insertionHeap);
                var node = nodes[heapItem.Id];

                var union = node->box.Union(bounds);
                var directCost = union.Area();
                var cost = directCost + heapItem.Cost;

                if (cost < bestCost) {
                    bestCost = cost;
                    bestIndex = heapItem.Id;
                }

                var extraInheritedCost = union.Area() - node->box.Area();
                var totalInheritedCost = heapItem.Cost + extraInheritedCost;

                var lowerBoundChildrenCost = bounds.Area() + totalInheritedCost;

                if (lowerBoundChildrenCost < bestCost) {
                    if (node->child1 != InvalidNode) {
                        UnsafeMinHeap.Push(ref insertionHeap, new UnsafeMinHeap.HeapItem {Id = node->child1, Cost = totalInheritedCost});
                    }
                    if (node->child2 != InvalidNode) {
                        UnsafeMinHeap.Push(ref insertionHeap, new UnsafeMinHeap.HeapItem {Id = node->child2, Cost = totalInheritedCost});
                    }
                }
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (bestIndex <= InvalidNode) {
                throw new InvalidOperationException();
            }
#endif

            var sibling = bestIndex;
            
            // Stage 2: create a new parent
            int oldParent = nodes[sibling]->parentIndex;
            int newParent = AllocInternalNode();
            nodes[newParent]->parentIndex = oldParent;
            nodes[newParent]->box = bounds.Union(nodes[sibling]->box);
            if (oldParent != InvalidNode) {
                // The sibling was not the root
                if (nodes[oldParent]->child1 == sibling) {
                    nodes[oldParent]->child1 = newParent;
                } else {
                    nodes[oldParent]->child2 = newParent;
                }
            } else { 
                // The sibling was the root
                rootIndex[0] = newParent;
            }

            nodes[newParent]->child1 = sibling;
            nodes[newParent]->child2 = leafIndex;
            nodes[sibling]->parentIndex = newParent;
            nodes[leafIndex]->parentIndex = newParent;
            
            // Stage 3: walk back up the tree refitting AABBs
            RefitParents(nodes[leafIndex]->parentIndex);

            return leafIndex;
        }

        public void RemoveLeaf(int index) {
            var node = nodes[index];
            var parent = nodes[node->parentIndex];
            var siblingIndex = parent->child1 == index ? parent->child2 : parent->child1;
            
            if (node->parentIndex != InvalidNode) {
                // Node is not the root
                if (parent->parentIndex != InvalidNode) {
                    // Parent also not the root
                    var grandParent = nodes[parent->parentIndex];

                    if (grandParent->child1 == node->parentIndex) {
                        grandParent->child1 = siblingIndex;
                    } else {
                        grandParent->child2 = siblingIndex;
                    }
                    nodes[siblingIndex]->parentIndex = parent->parentIndex;
                } else {
                    // Parent is the root
                    // So make sibling the root instead
                    nodes[siblingIndex]->parentIndex = InvalidNode;
                    rootIndex[0] = siblingIndex;
                }

                if (!nodes[node->parentIndex]->isLeaf) {
                    RefitParents(node->parentIndex);
                }
            } else {
                rootIndex[0] = InvalidNode;
            }
            
            if (node->parentIndex != InvalidNode) {
                DeallocNode(node->parentIndex);
            }
            DeallocNode(index);
        }

        public void Reinsert(int index, Collider collider, RigidTransform transform, uint layer = 0xffffffff) {
            RemoveLeaf(index);
            var newIndex = InsertLeaf(collider, transform, layer); 
            Assert.AreEqual(index, newIndex);
        }

        private void RefitParents(int index) {
            while (index != InvalidNode) {
                int child1 = nodes[index]->child1;
                int child2 = nodes[index]->child2;
                nodes[index]->box = nodes[child1]->box.Union(nodes[child2]->box);
                
                //TODO: Mark node as dirty and set grandchildrenAabbs in a later single pass
                nodes[index]->grandchildrenAabbs.SetAabb(0, nodes[nodes[child1]->child1]->box);
                nodes[index]->grandchildrenAabbs.SetAabb(1, nodes[nodes[child1]->child2]->box);
                nodes[index]->grandchildrenAabbs.SetAabb(2, nodes[nodes[child2]->child1]->box);
                nodes[index]->grandchildrenAabbs.SetAabb(3, nodes[nodes[child2]->child2]->box);
                
                // Balance whilst refitting
                TreeRotations.RotateOptimize(ref this, index);
                
                index = nodes[index]->parentIndex;
            }
        }
        
        private int AllocLeafNode(ref Leaf leaf) {
            var box = leaf.collider.CalculateBounds(leaf.transform);
            // Expand a bit for some room for movement without an update. TODO: proper implementation
            box.Expand(config.BoundsExpansion); 
            var node = new Node {
                box = box,
                leaf = leaf,
                isLeaf = true,
            };
            var id = nodesList->Add(node);
            return id;
        }
        
        private int AllocInternalNode() {
            var node = new Node {
                isLeaf = false
            };
            var id = nodesList->Add(node);
            return id;
        }
        
        private void DeallocNode(int index) {
            nodesList->RemoveAt(index);
        }

        public void Dispose() {
            UnsafeNodesList.Destroy(nodesList);
            nodesList = null;
            rootIndex.Dispose();
        }

        public int DebugGetRootNodeIndex() {
            return rootIndex.IsCreated ? rootIndex[0] : 0;
        }

        public void DebugGetAllChildren(int index, NativeList<int> indices, bool leavesOnly = false) {
            if (index != InvalidNode) {
                var node = nodes[index];
                if (node->isLeaf || !leavesOnly) {
                    indices.Add(index);
                }
                
                DebugGetAllChildren(node->child2, indices, leavesOnly);
                DebugGetAllChildren(node->child1, indices, leavesOnly);
            }
        }
        
        public Node DebugGetNode(int index) {
            return *nodes[index];
        }
        
        public int DebugGetTotalNodesLength() {
            return nodes.length;
        }
    }
}
