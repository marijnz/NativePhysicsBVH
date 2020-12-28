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
		private UnsafeNodesList* nodes;
		
		private NativeArray<int> rootIndex;
		
		private UnsafeMinHeap insertionHeap;

		public NativeBVHTree(int initialCapacity = 64, Allocator allocator = Allocator.Temp) : this() {
			nodes = UnsafeNodesList.Create<Node>(initialCapacity, allocator, NativeArrayOptions.ClearMemory);
			
			rootIndex = new NativeArray<int>(1, allocator);

			// Create invalid node (at index 0)
			AllocInternalNode();
			
			insertionHeap = new UnsafeMinHeap(initialCapacity, allocator);
		}

		public int InsertLeaf(Collider collider, uint layer = 0xffffffff) {
			insertionHeap.Clear();
			
			var leafIndex = AllocLeafNode(collider, layer);
			var bounds = GetNode(leafIndex)->box;
			
			if (nodes->length == 2) {
				rootIndex[0] = leafIndex;
				return leafIndex;
			}
			
			// Stage 1: find the best sibling for the new leaf
			float bestCost = float.MaxValue;
			int bestIndex = -1;
			insertionHeap.Push(new UnsafeMinHeap.HeapItem {Id = rootIndex[0], Cost = 0});

			while (insertionHeap.Count != 0) {
				var heapItem = insertionHeap.Pop();
				var node = GetNode(heapItem.Id);

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
			int oldParent = GetNode(sibling)->parentIndex;
			int newParent = AllocInternalNode();
			GetNode(newParent)->parentIndex = oldParent;
			GetNode(newParent)->box = bounds.Union(GetNode(sibling)->box);
			if (oldParent != InvalidNode) {
				// The sibling was not the root
				if (GetNode(oldParent)->child1 == sibling) {
					GetNode(oldParent)->child1 = newParent;
				} else {
					GetNode(oldParent)->child2 = newParent;
				}
			} else { 
				// The sibling was the root
				rootIndex[0] = newParent;
			}

			GetNode(newParent)->child1 = sibling;
			GetNode(newParent)->child2 = leafIndex;
			GetNode(sibling)->parentIndex = newParent;
			GetNode(leafIndex)->parentIndex = newParent;
			
			// Stage 3: walk back up the tree refitting AABBs
			RefitParents(GetNode(leafIndex)->parentIndex);

			return leafIndex;
		}

		public void RemoveLeaf(int index) {
			var node = GetNode(index);
			var parent = GetNode(node->parentIndex);
			var siblingIndex = parent->child1 == index ? parent->child2 : parent->child1;
			
			if (node->parentIndex != InvalidNode) {
				// Node is not the root
				if (parent->parentIndex != InvalidNode) {
					// Parent also not the root
					var grandParent = GetNode(parent->parentIndex);

					if (grandParent->child1 == node->parentIndex) {
						grandParent->child1 = siblingIndex;
					} else {
						grandParent->child2 = siblingIndex;
					}
					GetNode(siblingIndex)->parentIndex = parent->parentIndex;
				} else {
					// Parent is the root
					// So make sibling the root instead
					GetNode(siblingIndex)->parentIndex = InvalidNode;
					rootIndex[0] = siblingIndex;
				}

				if (!GetNode(node->parentIndex)->isLeaf) {
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
				int child1 = GetNode(index)->child1;
				int child2 = GetNode(index)->child2;
				GetNode(index)->box = GetNode(child1)->box.Union(GetNode(child2)->box);
				
				GetNode(index)->grandchildrenAabbs.SetAabb(0, GetNode(GetNode(child1)->child1)->box);
				GetNode(index)->grandchildrenAabbs.SetAabb(1, GetNode(GetNode(child1)->child2)->box);
				GetNode(index)->grandchildrenAabbs.SetAabb(2, GetNode(GetNode(child2)->child1)->box);
				GetNode(index)->grandchildrenAabbs.SetAabb(3, GetNode(GetNode(child2)->child2)->box);
				
				//TODO: Rotations
				
				index = GetNode(index)->parentIndex;
			}
		}

		private Node* GetNode(int index) => nodes->Get<Node>(index);

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
			var id = nodes->Add<Node>(node);
			return id;
		}
		
		private int AllocInternalNode() {
			var node = new Node {
				isLeaf = false
			};
			var id = nodes->Add(node);
			return id;
		}
		
		private void DeallocNode(int index) {
			nodes->RemoveAt<Node>(index);
		}

		public void Dispose() {
			UnsafeNodesList.Destroy(nodes);
			nodes = null;
			insertionHeap.Dispose();
			rootIndex.Dispose();
		}

		public int DebugGetRootNodeIndex() {
			return rootIndex.IsCreated ? rootIndex[0] : 0;
		}
		
		public Node DebugGetNode(int index) {
			return *GetNode(index);
		}
		
		public int DebugGetTotalNodesLength() {
			return nodes->length;
		}
	}
}
