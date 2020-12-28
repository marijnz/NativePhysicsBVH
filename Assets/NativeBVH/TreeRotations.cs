namespace NativeBVH {
    /// <summary>
    /// Balancing of tree per:
    /// - https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf
    /// - A. Kensler (2008) - Tree Rotations for Improving Bounding Volume Hierarchies
    /// </summary>
    public static unsafe class TreeRotations {
        public static void RotateOptimize(ref NativeBVHTree tree, int index) {
            var node = tree.nodes[index];
            if (node->parentIndex != NativeBVHTree.InvalidNode) {
                var parent = tree.nodes[node->parentIndex];
                if (parent->parentIndex != NativeBVHTree.InvalidNode) {
                    var grandParent = tree.nodes[parent->parentIndex];

                    // Four potential rotations, try them all
                    TryRotate(ref tree, tree.nodes[grandParent->child1]->child1, tree.nodes[grandParent->child1]->child2, grandParent->child1, grandParent->child2);
                    TryRotate(ref tree, tree.nodes[grandParent->child1]->child2, tree.nodes[grandParent->child1]->child1, grandParent->child1, grandParent->child2);
                    TryRotate(ref tree, tree.nodes[grandParent->child2]->child1, tree.nodes[grandParent->child2]->child2, grandParent->child2, grandParent->child1);
                    TryRotate(ref tree, tree.nodes[grandParent->child2]->child2, tree.nodes[grandParent->child2]->child1, grandParent->child2, grandParent->child1);
                }
            }
        }

        private static void TryRotate(ref NativeBVHTree tree, int fromChild, int siblingChild, int fromParent, int toParent) {
            if (fromChild != NativeBVHTree.InvalidNode) {
                if (tree.nodes[siblingChild]->box.Union(tree.nodes[toParent]->box).Area() < tree.nodes[fromParent]->box.Area()) {
                    Rotate(ref tree, fromChild, toParent);
                }
            }
        }

        private static void Rotate(ref NativeBVHTree tree, int fromIndex, int toIndex) {
            var from = tree.nodes[fromIndex];
            var to = tree.nodes[toIndex];
			
            var fromParentIndex = from->parentIndex;
            from->parentIndex = to->parentIndex;
            to->parentIndex = fromParentIndex;
            
            UpdateParent(ref tree, toIndex, fromIndex);
            UpdateParent(ref tree, fromIndex, toIndex);
        }
		
        private static void UpdateParent(ref NativeBVHTree tree, int fromIndex, int toIndex) {
            var node = tree.nodes[toIndex];
            if (tree.nodes[node->parentIndex]->child1 == fromIndex) {
                tree.nodes[node->parentIndex]->child1 = toIndex;
            } else {
                tree.nodes[node->parentIndex]->child2 = toIndex;
            }
        }
    }
}