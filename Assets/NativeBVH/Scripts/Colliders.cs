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
        
        public AABB3D CalculateBounds(RigidTransform transform = default) {
            fixed (Collider* target = &this) {
                switch (type) {
                    case Type.Box:
                        return ((BoxCollider*) target->data)->CalculateBounds(transform);
                    case Type.Sphere:
                        return ((SphereCollider*) target->data)->CalculateBounds(transform);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    default:
                        throw new ArgumentOutOfRangeException();
#endif
                }
            }
        }
        
        public void DebugDraw(RigidTransform transform) {
            var prevMatrics = Gizmos.matrix;
            try {
                Gizmos.matrix = Matrix4x4.TRS(transform.pos, transform.rot, Vector3.one);;
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
            } finally {
                Gizmos.matrix = prevMatrics;
            }
        }
    }

    public struct BoxCollider {
        public float3 center;
        public float3 size;
        
        public float3 LowerBound => center - size * .5f;
        public float3 UpperBound => center + size * .5f;

        public static unsafe Collider Create(float3 center, float3 size) {
            var boxCollider = new BoxCollider {center = center, size = size};
            var collider = new Collider {type = Collider.Type.Box};
            (*(BoxCollider*) collider.data) = boxCollider;
            return collider;
        }
        
        public AABB3D CalculateBounds(RigidTransform transform) {
            // From Unity.Physics
            float3 centerInB = math.transform(transform, center);
            float3 x = math.mul(transform.rot, new float3(size.x * 0.5f, 0, 0));
            float3 y = math.mul(transform.rot, new float3(0, size.y * 0.5f, 0));
            float3 z = math.mul(transform.rot, new float3(0, 0, size.z * 0.5f));
            float3 halfExtentsInB = math.abs(x) + math.abs(y) + math.abs(z);
            
            return new AABB3D {
                LowerBound = centerInB - halfExtentsInB,
                UpperBound = centerInB + halfExtentsInB
            };
        }

        public bool CastRay(NativeBVHTree.Ray ray) {
            var invD = 1 / ray.direction;
            return IntersectionUtils.DoesOverlap(LowerBound, UpperBound, ref ray, invD);
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
        
        public AABB3D CalculateBounds(RigidTransform rigidTransform) {
            return new AABB3D {
                LowerBound = rigidTransform.pos + center - new float3(radius),
                UpperBound = rigidTransform.pos + center + new float3(radius)
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
