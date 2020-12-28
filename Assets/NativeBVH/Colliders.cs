using System;
using Unity.Mathematics;
using UnityEngine;

namespace NativeBVH {
	public unsafe struct Collider {
		public enum Type {
			Box, 
			Sphere
		}
		public Type type;
		
		// Inlining primitive data
		public fixed byte data[24];

		public bool CastRay(NativeBVHTree.Ray ray) {
			fixed (Collider* target = &this) {
				switch (type) {
					case Type.Box:
						return ((BoxCollider*) target->data)->CastRay(ray);
					case Type.Sphere:
						return ((SphereCollider*) target->data)->CastRay(ray);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					default:
						throw new ArgumentOutOfRangeException();
#endif
				}
			}
		}
		
		public AABB3D CalculateBounds() {
			fixed (Collider* target = &this) {
				switch (type) {
					case Type.Box:
						return ((BoxCollider*) target->data)->CalculateBounds();
					case Type.Sphere:
						return ((SphereCollider*) target->data)->CalculateBounds();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					default:
						throw new ArgumentOutOfRangeException();
#endif
				}
			}
		}
		
		public void DebugDraw() {
			fixed (Collider* target = &this) {
				switch (type) {
					case Type.Box:
						var box = ((BoxCollider*) target->data);
						Gizmos.DrawCube(box->center, box->size);
						break;
					case Type.Sphere:
						var sphere = ((SphereCollider*) target->data);
						Gizmos.DrawSphere(sphere->center, sphere->radius);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}

	public struct BoxCollider {
		public float3 center;
		public float3 size;
		//public quaternion orientation; //TODO?
		
		public float3 LowerBound => center - size * .5f;
		public float3 UpperBound => center + size * .5f;

		public static unsafe Collider Create(float3 center, float3 size) {
			var boxCollider = new BoxCollider {center = center, size = size};
			var collider = new Collider {type = Collider.Type.Box};
			(*(BoxCollider*) collider.data) = boxCollider;
			return collider;
		}
		
		public AABB3D CalculateBounds() {
			return new AABB3D {
				LowerBound = center - size * .5f,
				UpperBound = center + size * .5f
			};
		}

		public bool CastRay(NativeBVHTree.Ray ray) {
			var invD = 1 / ray.direction;
			return IntersectionUtils.Overlap(LowerBound, UpperBound, ref ray, invD);
		}
	}

	public struct SphereCollider {
		public float3 center;
		public float radius;
		
		public static unsafe Collider Create(float3 center, float radius) {
			var sphereCollider = new SphereCollider {center = center, radius = radius};
			var collider = new Collider {type = Collider.Type.Sphere};
			(*(SphereCollider*) collider.data) = sphereCollider;
			return collider;
		}
		
		public AABB3D CalculateBounds() {
			return new AABB3D {
				LowerBound = center - new float3(radius),
				UpperBound = center + new float3(radius)
			};
		}

		/// <summary>
		/// Per: Christer Ericson - Real-Time Collision Detection (p. 179)
		/// </summary>
		public bool CastRay(NativeBVHTree.Ray ray) {
			var m = ray.origin - center;
			var c = math.dot(m, m) - radius * radius;
			// If there is definitely at least one real root, there must be an intersection
			if (c <= 0.0f) {
				return true;
			}
			var b = math.dot(m, ray.direction);
			// Early exit if ray origin outside sphere and ray pointing away from sphere
			if (b > 0.0f) {
				return false;
			}
			var disc = b * b - c;
			// A negative discriminant corresponds to ray missing sphere
			if (disc < 0.0f) {
				return false;
			}
			// Now ray must hit sphere
			return true;
		}
	}
}
