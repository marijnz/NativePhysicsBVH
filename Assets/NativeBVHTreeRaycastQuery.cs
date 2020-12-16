using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
			
			var stack = new UnsafeStack<int>(Allocator.Temp);
			stack.Push(rootIndex[0]);
			
			while (stack.Count > 0) {
				int index = stack.Pop();
				if (!Overlap(ref GetNode(index)->Box, ref ray, invD)) {
					continue;
				}

				if (GetNode(index)->IsLeaf) {
					// TODO: Support objects
					results.Add(index);
				} else {
					stack.Push(GetNode(index)->Child1);
					stack.Push(GetNode(index)->Child2);
				}
			}
			
			stack.Dispose();
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
