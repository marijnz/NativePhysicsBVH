using Unity.Mathematics;

namespace NativeBVH {
    public class IntersectionUtils {
        /// <summary>
        /// per https://medium.com/@bromanz/another-view-on-the-classic-ray-aabb-intersection-algorithm-for-bvh-traversal-41125138b525
        /// </summary>
        public static bool Overlap(float3 boxMin, float3 boxMax, ref NativeBVHTree.Ray ray, float3 invD) {
            var t0s = (boxMin - ray.origin) * invD;
            var t1s = (boxMax - ray.origin) * invD;

            var tsmaller = math.min(t0s, t1s);
            var tbigger  = math.max(t0s, t1s);

            var tmin = math.max(ray.minDistance, math.max(tsmaller[0], math.max(tsmaller[1], tsmaller[2])));
            var tmax = math.min(ray.maxDistance, math.min(tbigger[0], math.min(tbigger[1], tbigger[2])));

            return (tmin < tmax);
        }
    }
}