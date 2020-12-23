﻿using System;
using Unity.Collections;
using Unity.Mathematics;

namespace NativeBVH {
    public unsafe partial struct NativeBVHTree : IDisposable {
        public struct Ray {
            public float3 origin;
            public float3 direction;
            public float minDistance;
            public float maxDistance;
        }
		
        public void RayCast(Ray ray, NativeList<int> results) {
            var invD = 1 / ray.direction;
            var stack = stackalloc int[256];
            stack[0] = rootIndex[0];
            var top = 1;

            while (top > 0) {
                var index = stack[--top];
                var node = GetNode(index);

                if (!IntersectionUtils.Overlap(node->Box.LowerBound, node->Box.UpperBound, ref ray, invD)) {
                    continue;
                }
                
                if (node->IsLeaf) {
                    if (node->Collider.CastRay(ray)) {
                        results.Add(index);
                    }
                } else {
                    stack[top++] = node->Child1;
                    stack[top++] = node->Child2;
                }
            }
        }
    }
}