using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace NativeBVH.Editor {
    [ExecuteInEditMode]
    public class NativeBVHDebugDrawer : MonoBehaviour {
        public bool HideInternalNodes;
        public bool HideLeafNodes;
        public bool HideColliderPrimitives = true;
        
        public static NativeBVHTree LastTree;
        public static int[] LastTreeRayHits;
        public static bool[] LastTreeRayVisited;

        private int leafCount = 0;
        private int maxDepth = 0;
        public static NativeBVHTree.Ray LastRay;

        public void OnDrawGizmos() {
            leafCount = 0;
            Draw(LastTree.DebugGetRootNodeIndex(), 1);
            Handles.Label(Vector3.zero, "Leaf count: " + leafCount + " Max depth: " + maxDepth);

            if (math.any(LastRay.direction != float3.zero)) {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(LastRay.origin, LastRay.origin + math.normalize(LastRay.direction) * LastRay.maxDistance);
            }
        }

        private void Draw(int nodeIndex, int depth) {
            if (nodeIndex == NativeBVHTree.InvalidNode) {
                return;
            }
            maxDepth = math.max(maxDepth, depth);
            var node = LastTree.DebugGetNode(nodeIndex);

            var box = node.box;
            box.LowerBound -= node.isLeaf ? 0 : 0.2f;
            box.UpperBound += node.isLeaf ? 0 : 0.2f;

            var size = (box.UpperBound - box.LowerBound);
            var center = box.LowerBound + size / 2;

            bool isHit = (LastTreeRayHits != null && LastTreeRayHits.Contains(nodeIndex));
            bool isVisited = (LastTreeRayVisited != null && LastTreeRayVisited[nodeIndex]);

            if ((node.isLeaf && !HideLeafNodes) || (!node.isLeaf && !HideInternalNodes)) {
                if (isHit) {
                    Gizmos.color = Color.red;
                } else if (isVisited) {
                    Gizmos.color = Color.yellow;
                } else {
                    Gizmos.color = node.isLeaf ? Color.white : Color.green;
                }
                Gizmos.DrawWireCube(new Vector3(center.x, center.y, center.z), new Vector3(size.x, size.y, size.z));
                Handles.Label(new Vector3( box.LowerBound.x,  box.LowerBound.y, box.LowerBound.z), nodeIndex.ToString());
            }

            if (!HideColliderPrimitives && node.isLeaf) {
                node.collider.DebugDraw();
            }
            
            Draw(node.child1, ++depth);
            Draw(node.child2, ++depth);
            if (node.isLeaf) {
                leafCount++;
            }
        }
    }
}