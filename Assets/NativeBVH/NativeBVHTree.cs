using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NativeBVH {

	public struct Node {
		public int parentIndex;
		//public bool isLeaf;
		//public AABB3D box; // For leaf, this contains the primitive. For internal, it's the min/max of all children

		public CustomFourTransposedAabbs boxes;
		public int4 children; // -1 is leaf, 0 is not set, > 0 is internal
		//public bool4 isLeaves;
	}

	struct ColliderData {
		public Collider collider;
		public uint layer;
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
		public const int LeafNode = -1;

		
		[NativeDisableUnsafePtrRestriction]
		private UnsafeNodesList* nodes;
		
		private NativeArray<int> rootIndex;
		
		private UnsafeMinHeap insertionHeap;

		public NativeBVHTree(int initialCapacity = 64, Allocator allocator = Allocator.Temp) : this() {
			nodes = UnsafeNodesList.Create<Node>(initialCapacity, allocator, NativeArrayOptions.ClearMemory);
			
			rootIndex = new NativeArray<int>(1, allocator);

			// Create invalid node (at index 0)
			AllocNode();
			
			insertionHeap = new UnsafeMinHeap(initialCapacity, allocator);
		}

		public int InsertLeaf(Collider collider, uint layer = 0xffffffff) {
			insertionHeap.Clear();
			
			if (rootIndex[0] == InvalidNode) {
				// Create root
				var rootNodeIndex = AllocNode();
				var rootNode = GetNode(rootNodeIndex);
				rootNode->children[0] = -1;
				rootNode->boxes.SetAabb(0, collider.CalculateBounds());
				rootIndex[0] = rootNodeIndex;
				return rootNodeIndex;
			}
			
			//var leafIndex = AllocNode(collider, layer);
			var leafBounds = collider.CalculateBounds(); //GetNode(leafIndex)->box;
			
			// Stage 1: find the best sibling for the new leaf
			float bestCost = GetNode(rootIndex[0])->boxes.UnionToTransposed(leafBounds).Area();
			int bestIndex = rootIndex[0];
			int bestIndexChildIndex = 0;
			insertionHeap.Push(new UnsafeMinHeap.HeapItem {Id = rootIndex[0], InheritedCost = 0});

			while (insertionHeap.Count != 0) {
				var heapItem = insertionHeap.Pop();
				var node = GetNode(heapItem.Id);

				var union = node->boxes.UnionToTransposed(leafBounds);
				var childCosts = union.Areas();
				var childExtraInheritedCost = childCosts - node->boxes.Areas();

				var inheritedCost = heapItem.InheritedCost;

				for (int i = 0; i < 4; i++) {
					if (node->children[i] == InvalidNode) {
						continue;
					}
					
					var totalChildCost = childCosts[i] + inheritedCost;
					if (node->children[i] <= 0 && totalChildCost < bestCost) {
						bestIndex = heapItem.Id; // NOTE: Using the parent index here
						bestCost = totalChildCost;
						bestIndexChildIndex = i;
					}
					var totalInheritedCost = inheritedCost + childExtraInheritedCost[i];
					var childLowerBoundCost = leafBounds.Area() + totalInheritedCost;
					if (childLowerBoundCost < bestCost) {
						insertionHeap.Push(new UnsafeMinHeap.HeapItem {Id = node->children[i], InheritedCost = totalInheritedCost});
					}
				}
			}


			var bestNode = GetNode(bestIndex);

			if (math.any(bestNode->children == InvalidNode)) {
				// Got a free spot
				for (int i = 0; i < 4; i++) {
					if (bestNode->children[i] == InvalidNode) {
						bestNode->children[i] = -1;
						bestNode->boxes.SetAabb(i, leafBounds);
						RefitParents(bestNode->parentIndex); // TODO: Use parent here instead? bestIndex is already updated?
						break;
					}
				}
				
			} else {
				var bestNodeChild = bestNode->children[bestIndexChildIndex];
				if (bestNodeChild == LeafNode) {
					// Have to make an internal node
					int newIndex = AllocNode();
					var newNode = GetNode(newIndex);
					//TODO very temp! Should actually ignore not-set aabbs!
					newNode->boxes.SetAllAabbs(leafBounds);
					newNode->parentIndex = bestIndex;
					
					newNode->children[0] = -1; // TODO: Should here be checked if they should be childed?
					newNode->children[1] = -1;
					newNode->boxes.SetAabb(0, bestNode->boxes.GetAabb(bestIndexChildIndex));
					newNode->boxes.SetAabb(1, leafBounds);

					bestNode->children[bestIndexChildIndex] = newIndex;
					
					RefitParents(bestIndex);
				} else if(bestNodeChild > 0) {
					
					
					
					// It's an internal
						// IF free spot: use that
						// IF no free spot: least worst option?
						throw new InvalidOperationException();
				} else {
					throw new InvalidOperationException();
				}
				// Only leaves, make an internal node, pick best parent?
			}

			// Stage 3: walk back up the tree refitting AABBs
			
			return -1; // TODO return a tracking id?
		}

		public void RemoveLeaf(int index) {
			/*
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
			*/
		}

		private void RefitParents(int parentIndex) {
			while (parentIndex != InvalidNode) {

				var parentNode = GetNode(parentIndex);

				for (int i = 0; i < 4; i++) { // TODO optimize
					var childBounds = GetNode(parentIndex)->children[i] == LeafNode
						? GetNode(parentIndex)->boxes.GetAabb(i)
						: GetNode(GetNode(parentIndex)->children[i])->boxes.GetCompoundAabb();
				
					parentNode->boxes.SetAabb(i, childBounds);
				}
				
				
				parentIndex = parentNode->parentIndex;

				/*
				// Go next
				parentChildIndex = -1;
				var grandParentNode = GetNode(parentNode->parentIndex);
				for (int i = 0; i < 4; i++) {
					if (grandParentNode->children[i] == parentIndex) {
						parentChildIndex = i; 
						break;
					}
				}
				if (parentChildIndex == -1) {
					throw new InvalidOperationException();
				}
				*/
				//TODO: Rotations
			}
		}

		private Node* GetNode(int index) => nodes->Get<Node>(index);

		/*
		private int AllocLeafNode(Collider collider, uint layer) {
			var box = collider.CalculateBounds();
			// Expand a bit for some room for movement without an update. TODO: proper implementation
			//box.Expand(0.2f); 
			var node = new Node {
				leafBox = box,
				collider = collider,
				isLeaf = true,
				layer = layer
			};
			var id = nodes->Add<Node>(node);
			return id;
		}*/
		
		private int AllocNode() {
			var node = new Node {
				
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
