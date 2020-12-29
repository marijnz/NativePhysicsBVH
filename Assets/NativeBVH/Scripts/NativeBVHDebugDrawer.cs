using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace NativeBVH.Editor {
    [ExecuteInEditMode]
    public class NativeBVHDebugDrawer : MonoBehaviour {
        public bool HideInternalNodes = true;
        public bool HideLeafNodes = true;
        public bool HideColliderPrimitives = false;
        public bool ShowHitsOnly = false;
        
        // Limit to node & parent traversal
        public int LimitToId;
        public int ParentDepth = 1;

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

        private void Draw(int nodeIndex, int depth, bool drawingUpwards = false) {
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

            if (isHit) {
                Gizmos.color = Color.red;
            } else if (isVisited) {
                Gizmos.color = Color.yellow;
            } else {
                Gizmos.color = node.isLeaf ? Color.white : Color.green;
            }

            if (drawingUpwards || ((!ShowHitsOnly || isHit) && (LimitToId == 0 || LimitToId == nodeIndex))) {
                if ((node.isLeaf && !HideLeafNodes) || (!node.isLeaf && !HideInternalNodes)) {
                    Gizmos.DrawWireCube(new Vector3(center.x, center.y, center.z), new Vector3(size.x, size.y, size.z));
                    Handles.Label(new Vector3( box.LowerBound.x,  box.LowerBound.y, box.LowerBound.z), nodeIndex.ToString());
                }

                if (!HideColliderPrimitives && node.isLeaf) {
                    node.collider.DebugDraw(node.box.Center);
                }

                if (LimitToId == nodeIndex) {
                    // Draw parents
                    int count = 0;
                    var parent = node.parentIndex;
                    while (count++ < ParentDepth && parent != NativeBVHTree.InvalidNode) {
                        var parentNode = LastTree.DebugGetNode(parent);
                        Draw(parent, depth, true);
                        parent = parentNode.parentIndex;
                    }
                }
            }

            if (!drawingUpwards) {
                Draw(node.child1, ++depth);
                Draw(node.child2, ++depth);
            }
          
            if (node.isLeaf) {
                leafCount++;
            }
        }
    }
}