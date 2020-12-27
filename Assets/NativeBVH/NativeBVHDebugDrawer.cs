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
        public bool HideColliderPrimitives;
        public bool ShowHitsOnly;
        
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

            var aabb = node.boxes.GetCompoundAabb();
            if (!HideInternalNodes && (LimitToId == 0 || LimitToId == nodeIndex || drawingUpwards)) {
                {
                    Gizmos.color = Color.green;
                    var size = (aabb.UpperBound - aabb.LowerBound);
                    var center = aabb.LowerBound + size / 2;
                    Gizmos.DrawWireCube(new Vector3(center.x, center.y, center.z), new Vector3(size.x, size.y, size.z));
                    Handles.Label(new Vector3( aabb.LowerBound.x,  aabb.LowerBound.y, aabb.LowerBound.z), nodeIndex.ToString());
                }
            }

            for (int i = 0; i < 4; i++) {
                var child = node.children[i];
                if (child == NativeBVHTree.InvalidNode) {
                    continue;
                }

                var box = node.boxes.GetAabb(i);
                var isLeaf = node.children[i] == NativeBVHTree.LeafNode;
                
                box.LowerBound -= isLeaf ? 0 : 0.2f;
                box.UpperBound += isLeaf ? 0 : 0.2f;

                var size = (box.UpperBound - box.LowerBound);
                var center = box.LowerBound + size / 2;

                bool isHit = (LastTreeRayHits != null && LastTreeRayHits.Contains(nodeIndex));
                bool isVisited = (LastTreeRayVisited != null && LastTreeRayVisited[nodeIndex]);

                if (isHit) {
                    Gizmos.color = Color.red;
                } else if (isVisited) {
                    Gizmos.color = Color.yellow;
                } else {
                    Gizmos.color = isLeaf ? Color.white : Color.green;
                }

                if (drawingUpwards || ((!ShowHitsOnly || isHit) && (LimitToId == 0 || LimitToId == nodeIndex))) {
                    if ((isLeaf && !HideLeafNodes) || (!isLeaf && !HideInternalNodes)) {
                        Gizmos.DrawWireCube(new Vector3(center.x, center.y, center.z), new Vector3(size.x, size.y, size.z));
                        Handles.Label(new Vector3( box.LowerBound.x,  box.LowerBound.y, box.LowerBound.z), nodeIndex.ToString());
                    }

                    if (!HideColliderPrimitives && isLeaf) {
                        //node.collider.DebugDraw();
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
                    if (child > 0) {
                        Draw(child, ++depth);
                    }
                }
          
                if (isLeaf) {
                    leafCount++;
                }
                
            }
        }
    }
}