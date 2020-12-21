using System;
using Unity.Collections;
using Unity.Mathematics;

namespace NativeBVH {
    public unsafe partial struct NativeBVHTree : IDisposable {
        public struct Ray {
            public float3 Origin;
            public float3 Direction;
            public float MinDistance;
            public float MaxDistance;
        }
		
        public void RayCast(Ray ray, NativeList<int> results) {
            var invD = 1 / ray.Direction;
            var stack = stackalloc int[256];
            stack[0] = rootIndex[0];
            var top = 1;

            while (top > 0) {
                var index = stack[--top];
                var node = GetNode(index);

                if (!Overlap(ref node->Box, ref ray, invD)) {
                    continue;
                }
                
                if (node->IsLeaf) {
                    // TODO: Support objects
                    results.AddNoResize(index);
                } else {
                    stack[top++] = node->Child1;
                    stack[top++] = node->Child2;
                }
            }
        }


        /// <summary>
        /// per https://medium.com/@bromanz/another-view-on-the-classic-ray-aabb-intersection-algorithm-for-bvh-traversal-41125138b525
        /// </summary>
        private bool Overlap(ref AABB3D box, ref Ray ray, float3 invD) {
            var t0s = (box.LowerBound - ray.Origin) * invD;
            var t1s = (box.UpperBound - ray.Origin) * invD;

            var tsmaller = math.min(t0s, t1s);
            var tbigger  = math.max(t0s, t1s);

            var tmin = math.max(ray.MinDistance, math.max(tsmaller[0], math.max(tsmaller[1], tsmaller[2])));
            var tmax = math.min(ray.MaxDistance, math.min(tbigger[0], math.min(tbigger[1], tbigger[2])));

            return (tmin < tmax);
        }
    }
}