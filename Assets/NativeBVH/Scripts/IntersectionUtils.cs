using Unity.Mathematics;

namespace NativeBVH {
    public class IntersectionUtils {
        public static bool DoesOverlap(float3 boxMin, float3 boxMax, ref NativeBVHTree.Ray ray, float3 invD) {
            return IntersectTest(boxMin, boxMax, ray.origin, ray.minDistance, ray.maxDistance, invD, out _);
        }
        
        public static bool DoesOverlap(ref AABB3D box, ref NativeBVHTree.Ray ray, float3 invD) {
            return IntersectTest(box.LowerBound, box.UpperBound,ray.origin, ray.minDistance, ray.maxDistance, invD, out _);
        }
        
        public static bool IsInRange(float3 boxMin, float3 boxMax, float3 point, float maxDistanceSqrd) {
            var c = math.max(math.min(point,boxMax), boxMin);
            var d = math.distancesq(c, point);
            return d < maxDistanceSqrd;
        }
        
        /// <summary>
        /// per https://medium.com/@bromanz/another-view-on-the-classic-ray-aabb-intersection-algorithm-for-bvh-traversal-41125138b525
        /// </summary>
        public static bool IntersectTest(float3 boxMin, float3 boxMax, float3 origin, float minDist, float maxDist, float3 invD, out float t) {
            var t0s = (boxMin - origin) * invD;
            var t1s = (boxMax - origin) * invD;

            var tsmaller = math.min(t0s, t1s);
            var tbigger  = math.max(t0s, t1s);

            var tmin = math.max(minDist, math.max(tsmaller[0], math.max(tsmaller[1], tsmaller[2])));
            var tmax = math.min(maxDist, math.min(tbigger[0], math.min(tbigger[1], tbigger[2])));

            t = tmin;
            return (tmin < tmax);
        }


        
    }
}