using System;
using Unity.Collections;
using Unity.Mathematics;

namespace NativeBVH {
    public unsafe partial struct NativeBVHTree : IDisposable {
        public struct Ray {
            public float3 origin;
            public float3 direction;
            public float minDistance;
            public float maxDistance;
            public uint layerMask;
        }
		
        public void RayCast(Ray ray, NativeList<int> results) {
            var invD = 1 / ray.direction;
            ray.direction = math.normalize(ray.direction);
            
            var stack = stackalloc int[256];
            stack[0] = rootIndex[0];
            var top = 1;

            while (top > 0) {
                var index = stack[--top];
                var node = GetNode(index);

                if (!IntersectionUtils.Overlap(node->box.LowerBound, node->box.UpperBound, ref ray, invD)) {
                    continue;
                }
                
                if (node->isLeaf) {
                    if ((ray.layerMask & node->layer) == 0 && node->collider.CastRay(ray)) {
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