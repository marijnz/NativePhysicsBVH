using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace NativeBVH {

	public struct Node {
		public AABB3D Box;
		public Collider Collider;
		public int Layer;
		public int ParentIndex;
		public int Child1;
		public int Child2;
		public bool IsLeaf;
	}

	/// <summary>
	/// Implemented per https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf. WIP.
	/// </summary>
	public unsafe partial struct NativeBVHTree : IDisposable {
		public const int InvalidNode = 0;
		
		[NativeDisableUnsafePtrRestriction]
		private UnsafeList* nodes;
		
		private NativeArray<int> rootIndex;
		
		private UnsafeMinHeap insertionHeap;

		public NativeBVHTree(int initialCapacity = 64, Allocator allocator = Allocator.Temp) : this() {
			nodes = UnsafeList.Create(UnsafeUtility.SizeOf<Node>(),
				UnsafeUtility.AlignOf<Node>(),
				initialCapacity,
				allocator,
				NativeArrayOptions.ClearMemory);
			
			rootIndex = new NativeArray<int>(1, allocator);

			// Create invalid node (at index 0)
			AllocInternalNode();
			
			insertionHeap = new UnsafeMinHeap(initialCapacity, allocator);
		}

		public int InsertLeaf(Collider collider) {
			insertionHeap.Clear();
			
			var leafIndex = AllocLeafNode(collider);
			var bounds = GetNode(leafIndex)->Box;
			
			if (nodes->Length == 2) {
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

				var union = node->Box.Union(bounds);
				var directCost = union.Area();
				var cost = directCost + heapItem.Cost;

				if (cost < bestCost) {
					bestCost = cost;
					bestIndex = heapItem.Id;
				}

				var extraInheritedCost = union.Area() - node->Box.Area();
				var totalInheritedCost = heapItem.Cost + extraInheritedCost;

				var lowerBoundChildrenCost = bounds.Area() + totalInheritedCost;

				if (lowerBoundChildrenCost < bestCost) {
					if (node->Child1 != InvalidNode) {
						insertionHeap.Push(new UnsafeMinHeap.HeapItem {Id = node->Child1, Cost = totalInheritedCost});
					}
					if (node->Child2 != InvalidNode) {
						insertionHeap.Push(new UnsafeMinHeap.HeapItem {Id = node->Child2, Cost = totalInheritedCost});
					}
				}
			}

			var sibling = bestIndex;
			
			// Stage 2: create a new parent
			int oldParent = GetNode(sibling)->ParentIndex;
			int newParent = AllocInternalNode();
			GetNode(newParent)->ParentIndex = oldParent;
			GetNode(newParent)->Box = bounds.Union(GetNode(sibling)->Box);
			if (oldParent != InvalidNode) {
				// The sibling was not the root
				if (GetNode(oldParent)->Child1 == sibling) {
					GetNode(oldParent)->Child1 = newParent;
				} else {
					GetNode(oldParent)->Child2 = newParent;
				}
				GetNode(newParent)->Child1 = sibling;
				GetNode(newParent)->Child2 = leafIndex;
				GetNode(sibling)->ParentIndex = newParent;
				GetNode(leafIndex)->ParentIndex = newParent;
			} else { 
				// The sibling was the root
				GetNode(newParent)->Child1 = sibling;
				GetNode(newParent)->Child2 = leafIndex;
				GetNode(sibling)->ParentIndex = newParent;
				GetNode(leafIndex)->ParentIndex = newParent;
				rootIndex[0] = newParent;
			}

			// Stage 3: walk back up the tree refitting AABBs
			int index = GetNode(leafIndex)->ParentIndex;
			while (index != InvalidNode) {
				int child1 = GetNode(index)->Child1;
				int child2 = GetNode(index)->Child2;
				GetNode(index)->Box = GetNode(child1)->Box.Union(GetNode(child2)->Box);
				
				//TODO: Rotations
				
				index = GetNode(index)->ParentIndex;
			}

			return leafIndex;
		}
		
		private Node* GetNode(int index) => (Node*) ((long) nodes->Ptr + (long) index * sizeof (Node));

		private int AllocLeafNode(Collider collider) {
			var id = nodes->Length;
			var box = collider.CalculateBounds();
			// Expand a bit for some room for movement without an update. TODO: proper implementation
			box.Expand(0.2f); 
			var node = new Node {
				Box = box,
				Collider = collider,
				IsLeaf = true
			};
			nodes->Add(node);
			return id;
		}
		
		private int AllocInternalNode() {
			var id = nodes->Length;
			var node = new Node {
				IsLeaf = false
			};
			nodes->Add(node);
			return id;
		}

		public void Dispose() {
			nodes->Dispose();
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
	}
}
