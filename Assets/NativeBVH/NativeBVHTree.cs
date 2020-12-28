using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace NativeBVH {

	public struct Node {
		public AABB3D box;
		public Collider collider;
		public uint layer;
		public int parentIndex;
		public int child1;
		public int child2;
		public bool isLeaf;
		public SimpleFourTransposedAabbs grandchildrenAabbs;
	}

	/// <summary>
	/// Implemented per https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf. WIP.
	///
	/// Optimizations left:
	/// - Rotations (in optimize pass or insertion/deletion)
	/// - Make array linear (in optimize pass)
	/// </summary>
	public unsafe partial struct NativeBVHTree : IDisposable {
		public const int InvalidNode = 0;
		
		[NativeDisableUnsafePtrRestriction]
		private UnsafeNodesList* nodesList;

		internal UnsafeNodesList nodes => *nodesList;
		
		private NativeArray<int> rootIndex;
		
		private UnsafeMinHeap insertionHeap;

		public NativeBVHTree(int initialCapacity = 64, Allocator allocator = Allocator.Temp) : this() {
			nodesList = UnsafeNodesList.Create(initialCapacity, allocator, NativeArrayOptions.ClearMemory);
			
			rootIndex = new NativeArray<int>(1, allocator);

			// Create invalid node (at index 0)
			AllocInternalNode();
			
			insertionHeap = new UnsafeMinHeap(initialCapacity, allocator);
		}

		public int InsertLeaf(Collider collider, uint layer = 0xffffffff) {
			insertionHeap.Clear();
			
			var leafIndex = AllocLeafNode(collider, layer);
			var bounds = nodes[leafIndex]->box;
			
			if (nodes.length == 2) {
				rootIndex[0] = leafIndex;
				return leafIndex;
			}
			
			// Stage 1: find the best sibling for the new leaf
			float bestCost = float.MaxValue;
			int bestIndex = -1;
			insertionHeap.Push(new UnsafeMinHeap.HeapItem {Id = rootIndex[0], Cost = 0});

			while (insertionHeap.Count != 0) {
				var heapItem = insertionHeap.Pop();
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
						insertionHeap.Push(new UnsafeMinHeap.HeapItem {Id = node->child1, Cost = totalInheritedCost});
					}
					if (node->child2 != InvalidNode) {
						insertionHeap.Push(new UnsafeMinHeap.HeapItem {Id = node->child2, Cost = totalInheritedCost});
					}
				}
			}

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
		
		private int AllocLeafNode(Collider collider, uint layer) {
			var box = collider.CalculateBounds();
			// Expand a bit for some room for movement without an update. TODO: proper implementation
			//box.Expand(0.2f); 
			var node = new Node {
				box = box,
				collider = collider,
				isLeaf = true,
				layer = layer
			};
			var id = nodes.Add(node);
			return id;
		}
		
		private int AllocInternalNode() {
			var node = new Node {
				isLeaf = false
			};
			var id = nodes.Add(node);
			return id;
		}
		
		private void DeallocNode(int index) {
			nodes.RemoveAt(index);
		}

		public void Dispose() {
			UnsafeNodesList.Destroy(nodesList);
			nodesList = null;
			insertionHeap.Dispose();
			rootIndex.Dispose();
		}

		public int DebugGetRootNodeIndex() {
			return rootIndex.IsCreated ? rootIndex[0] : 0;
		}
		
		public Node DebugGetNode(int index) {
			return *nodes[index];
		}
		
		public int DebugGetTotalNodesLength() {
			return nodes.length;
		}
	}
}
