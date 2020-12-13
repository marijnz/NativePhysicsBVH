using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace NativeBVH {
	public struct Node {
		public AABB3D Box;
		//public int ObjectIndex; // TODO
		public int ParentIndex;
		public int Child1;
		public int Child2;
		public bool IsLeaf;
	}

	/// <summary>
	/// Implemented per https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf. WIP.
	/// </summary>
	public unsafe struct NativeBVHTree : IDisposable {
		public const int InvalidNode = 0;
		
		[NativeDisableUnsafePtrRestriction]
		private UnsafeList* nodes;
		
		private int rootIndex;
		
		private NativeArray<HeapItem> heap;

		public NativeBVHTree(Allocator allocator = Allocator.Temp, int initialCapacity = 64) : this() {
			nodes = UnsafeList.Create(UnsafeUtility.SizeOf<Node>(),
				UnsafeUtility.AlignOf<Node>(),
				initialCapacity,
				allocator,
				NativeArrayOptions.ClearMemory);

			// Create invalid node (at index 0)
			AllocInternalNode();
			
			heap = new NativeArray<HeapItem>(64, allocator);
		}

		public int InsertLeaf(AABB3D insertedLeaf) {
			UnsafeUtility.MemClear(heap.GetUnsafePtr(), sizeof(HeapItem) * heap.Length);
			
			var leafIndex = AllocLeafNode(insertedLeaf);
			
			if (nodes->Length == 0) {
				rootIndex = leafIndex;
				return leafIndex;
			}
			
			// Stage 1: find the best sibling for the new leaf
			float bestCost = GetNode(rootIndex)->Box.Union(insertedLeaf).Area();
			int bestIndex = rootIndex;
			push(new HeapItem {Id = rootIndex, InheritedCost = 0});

			while (Count != 0) {
				var heapItem = pop();
				var node = GetNode(heapItem.Id);

				var union = node->Box.Union(insertedLeaf);
				var directCost = union.Area();
				var cost = directCost + heapItem.InheritedCost;

				if (cost < bestCost) {
					bestCost = cost;
					bestIndex = heapItem.Id;
				}

				var extraInheritedCost = union.Area() - node->Box.Area();
				var totalInheritedCost = heapItem.InheritedCost + extraInheritedCost;

				var lowerBoundChildrenCost = insertedLeaf.Area() + totalInheritedCost;

				if (lowerBoundChildrenCost < cost) {
					if (node->Child1 != InvalidNode) {
						push(new HeapItem {Id = node->Child1, InheritedCost = totalInheritedCost});
					}
					if (node->Child2 != InvalidNode) {
						push(new HeapItem {Id = node->Child2, InheritedCost = totalInheritedCost});
					}
				}
			}

			var sibling = bestIndex;
			
			// Stage 2: create a new parent
			int oldParent = GetNode(sibling)->ParentIndex;
			int newParent = AllocInternalNode();
			GetNode(newParent)->ParentIndex = oldParent;
			GetNode(newParent)->Box = insertedLeaf.Union(GetNode(sibling)->Box);
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
				rootIndex = newParent;
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

		private int AllocLeafNode(AABB3D box) {
			var id = nodes->Length;
			var node = new Node {
				Box = box,
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
			UnsafeList.Destroy(nodes);
			nodes = null;
		}

		public int DebugGetRootNodeIndex() {
			return rootIndex;
		}
		
		public Node DebugGetNode(int index) {
			return *GetNode(index);
		}
		
		// TODO: Separate heap
		#region Heap (Prio Queue)

		public struct HeapItem
		{
			public int Id;
			public float InheritedCost;
		}

		static readonly HeapItem defaultEdge = default;
		public int Count { get; set; }

		public void push(HeapItem v)
		{
			// Usually this would be the place so increase size of heap if needed, but it's pre-allocated.
			heap[Count] = v;
			SiftUp(Count++);
		}

		public HeapItem pop()
		{
			var v = top();
			heap[0] = heap[--Count];
			if (Count > 0) SiftDown(0);
			return v;
		}

		public HeapItem top()
		{
			if (Count > 0) return heap[0];
			return defaultEdge;
		}

		void SiftUp(int n)
		{
			var v = heap[n];
			for (var n2 = n / 2; n > 0 && CompareCost(v, heap[n2]) > 0; n = n2, n2 /= 2) heap[n] = heap[n2];
			heap[n] = v;
		}
		void SiftDown(int n)
		{
			var v = heap[n];
			for (var n2 = n * 2; n2 < Count; n = n2, n2 *= 2)
			{
				if (n2 + 1 < Count && CompareCost(heap[n2 + 1], heap[n2]) > 0) n2++;
				if (CompareCost(v, heap[n2]) >= 0) break;
				heap[n] = heap[n2];
			}
			heap[n] = v;
		}

		int CompareCost(HeapItem a, HeapItem b)
		{
			if(a.InheritedCost < b.InheritedCost) return 1;
			if(a.InheritedCost > b.InheritedCost) return -1;
			return 0;
		}

		#endregion
	}
}
