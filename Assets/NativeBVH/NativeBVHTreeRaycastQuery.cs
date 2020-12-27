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
            int visits = 0;

            while (top > 0) {
                var index = stack[--top];
                var node = GetNode(index);

                var hits = node->boxes.Raycast(ray, float.MaxValue, out float4 fractions, invD);
                
                int4 hitData;
                int hitCount = math.compress((int*)(&hitData), 0, node->children, hits);
                visits += 4;

                for (int i = 0; i < hitCount; i++) {
                    var hitIndex = hitData[i];
                    if (hitIndex == LeafNode) {
                        //if ((ray.layerMask & node->layer) == 0 && node->collider.CastRay(ray)) {
                            results.Add(index);
                      //  } 
                    } else if (hitIndex > 0) {
                        stack[top++] = hitIndex;
                    }
                }
            } 
            var s = visits;
        }
    }
}