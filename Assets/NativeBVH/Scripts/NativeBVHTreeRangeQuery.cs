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
        
        public void DistanceQuery(DistanceQueryInput query, NativeList<int> results) {
            float maxDistanceSqrd = query.maxDistance * query.maxDistance;

            var stack = stackalloc int[TreeTraversalStackSize];
            stack[0] = rootIndex[0];
            var top = 1;
            
            while (top > 0) {
                var index = stack[--top];
                var node = nodes[index];

                // TODO: make SIMD
                if (!IntersectionUtils.IsInRange(ref node->box, query.origin, maxDistanceSqrd)) {
                    continue;
                }

                if (node->isLeaf) {
                    if ((query.layerMask & node->leaf.layer) == 0 /*TODO: distance check on primitive*/) {
                        results.Add(index);
                    } 
                } else {
                    stack[top++] = node->child1;
                    stack[top++] = node->child2;
                }
            }
        }
    }
}