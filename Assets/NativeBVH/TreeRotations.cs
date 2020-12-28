namespace NativeBVH {
    /// <summary>
    /// Balancing of tree per:
    /// - https://box2d.org/files/ErinCatto_DynamicBVH_GDC2019.pdf
    /// - A. Kensler (2008) - Tree Rotations for Improving Bounding Volume Hierarchies
    /// </summary>
    public static unsafe class TreeRotations {
        public static void RotateOptimize(ref NativeBVHTree tree, int index) {
            var node = tree.GetNode(index);
            if (node->parentIndex != NativeBVHTree.InvalidNode) {
                var parent = tree.GetNode(node->parentIndex);
                if (parent->parentIndex != NativeBVHTree.InvalidNode) {
                    var grandParent = tree.GetNode(parent->parentIndex);

                    // Four potential rotations, try them all
                    TryRotate(ref tree, tree.GetNode(grandParent->child1)->child1, tree.GetNode(grandParent->child1)->child2, grandParent->child1, grandParent->child2);
                    TryRotate(ref tree, tree.GetNode(grandParent->child1)->child2, tree.GetNode(grandParent->child1)->child1, grandParent->child1, grandParent->child2);
                    TryRotate(ref tree, tree.GetNode(grandParent->child2)->child1, tree.GetNode(grandParent->child2)->child2, grandParent->child2, grandParent->child1);
                    TryRotate(ref tree, tree.GetNode(grandParent->child2)->child2, tree.GetNode(grandParent->child2)->child1, grandParent->child2, grandParent->child1);
                }
            }
        }

        private static void TryRotate(ref NativeBVHTree tree, int fromChild, int siblingChild, int fromParent, int toParent) {
            if (fromChild != NativeBVHTree.InvalidNode) {
                if (tree.GetNode(siblingChild)->box.Union(tree.GetNode(toParent)->box).Area() < tree.GetNode(fromParent)->box.Area()) {
                    Rotate(ref tree, fromChild, toParent);
                }
            }
        }

        private static void Rotate(ref NativeBVHTree tree, int fromIndex, int toIndex) {
            var from = tree.GetNode(fromIndex);
            var to = tree.GetNode(toIndex);
			
            var fromParentIndex = from->parentIndex;
            from->parentIndex = to->parentIndex;
            to->parentIndex = fromParentIndex;
            
            UpdateParent(ref tree, toIndex, fromIndex);
            UpdateParent(ref tree, fromIndex, toIndex);
        }
		
        private static void UpdateParent(ref NativeBVHTree tree, int fromIndex, int toIndex) {
            var node = tree.GetNode(toIndex);
            if (tree.GetNode(node->parentIndex)->child1 == fromIndex) {
                tree.GetNode(node->parentIndex)->child1 = toIndex;
            } else {
                tree.GetNode(node->parentIndex)->child2 = toIndex;
            }
        }
    }
}