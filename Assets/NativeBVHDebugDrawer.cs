using UnityEditor;
using UnityEngine;

namespace NativeBVH.Editor {
    [ExecuteInEditMode]
    public class NativeBVHDebugDrawer : MonoBehaviour {
        public static NativeBVHTree LastTree;

        private int leafCount = 0;

        public void OnDrawGizmos() {
            leafCount = 0;
            Draw(LastTree.DebugGetRootNodeIndex());
            Handles.Label(Vector3.zero, "Leaf count: " + leafCount);
        }

        private void Draw(int nodeIndex) {
            if (nodeIndex == NativeBVHTree.InvalidNode) {
                return;
            }
            var node = LastTree.DebugGetNode(nodeIndex);

            var box = node.Box;
            box.LowerBound -= node.IsLeaf ? 0 : 0.2f;
            box.UpperBound += node.IsLeaf ? 0 : 0.2f;

            var size = (box.UpperBound - box.LowerBound);
            var center = box.LowerBound + size / 2;
            Gizmos.color = node.IsLeaf ? Color.white : Color.green;
            Gizmos.DrawWireCube(new Vector3(center.x, center.y, center.z), new Vector3(size.x, size.y, size.z));
            Handles.Label(new Vector3( box.LowerBound.x,  box.LowerBound.y, box.LowerBound.z), nodeIndex.ToString());
            Draw(node.Child1);
            Draw(node.Child2);
            if (node.IsLeaf) {
                leafCount++;
            }
        }
    }
}