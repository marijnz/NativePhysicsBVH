using System;
using Unity.Mathematics;

namespace NativeBVH {
	[Serializable]
	public struct AABB3D {
		public float3 LowerBound;
		public float3 UpperBound;
		
		public AABB3D(float3 lowerBound, float3 upperBound) {
			LowerBound = lowerBound;
			UpperBound = upperBound;
		}

		public AABB3D Union(AABB3D other) {
			return new AABB3D {
				LowerBound = math.min(other.LowerBound, LowerBound), 
				UpperBound = math.max(other.UpperBound, UpperBound)
			};
		}
		
		public float Area() {
			var d = UpperBound - LowerBound;
			return 2.0f * (d.x * d.y + d.y * d.z + d.z * d.x);
		}
	}
}