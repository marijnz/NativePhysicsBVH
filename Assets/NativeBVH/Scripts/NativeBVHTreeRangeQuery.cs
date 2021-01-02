using System;
using Unity.Collections;
using Unity.Mathematics;

namespace NativeBVH {
    public unsafe partial struct NativeBVHTree : IDisposable {
        public struct DistanceQueryInput {
            public float3 origin;
            public float maxDistance;
            public uint layerMask;
        }
        
        // TODO: make SIMD
        public void DistanceQuery(DistanceQueryInput query, NativeList<int> results) {
            float maxDistanceSqrd = query.maxDistance * query.maxDistance;

            var stack = stackalloc int[TreeTraversalStackSize];
            stack[0] = rootIndex[0];
            var top = 1;
            
            while (top > 0) {
                var index = stack[--top];
                var node = nodes[index];

                if (!IntersectionUtils.IsInRange(node->box.LowerBound, node->box.UpperBound, query.origin, maxDistanceSqrd)) {
                    continue;
                }

                if (node->isLeaf) {
                    if ((query.layerMask & node->leaf.layer) == 0 && DoDistanceQuery(query, ref node->leaf)) {
                        results.Add(index);
                    } 
                } else {
                    stack[top++] = node->child1;
                    stack[top++] = node->child2;
                }
            }
        }
        
        private bool DoDistanceQuery(DistanceQueryInput query, ref Leaf leaf) {
            // Translate distance query into node space
            var inverse = math.inverse(leaf.transform);
            query.origin = math.transform(inverse, query.origin); 
            
            // And cast the ray on the collider
            return leaf.collider.DistanceQuery(query);
        }
    }
}