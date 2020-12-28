 using Unity.Mathematics;

 namespace NativeBVH {
     /// <summary>
     /// Simplified version from Unity.Physics
     /// </summary>
    public struct SimpleFourTransposedAabbs {
        public float4 Lx, Hx;    // Lower and upper bounds along the X axis.
        public float4 Ly, Hy;    // Lower and upper bounds along the Y axis.
        public float4 Lz, Hz;    // Lower and upper bounds along the Z axis.

        public void SetAabb(int index, AABB3D aabb) {
            Lx[index] = aabb.LowerBound.x;
            Hx[index] = aabb.UpperBound.x;

            Ly[index] = aabb.LowerBound.y;
            Hy[index] = aabb.UpperBound.y;

            Lz[index] = aabb.LowerBound.z;
            Hz[index] = aabb.UpperBound.z;
        }
        
        public bool4 Raycast(NativeBVHTree.Ray ray, float3 invD) {
            float4 lx = Lx - new float4(ray.origin.x);
            float4 hx = Hx - new float4(ray.origin.x);
            float4 nearXt = lx * new float4(invD.x);
            float4 farXt = hx * new float4(invD.x);

            float4 ly = Ly - new float4(ray.origin.y);
            float4 hy = Hy - new float4(ray.origin.y);
            float4 nearYt = ly * new float4(invD.y);
            float4 farYt = hy * new float4(invD.y);

            float4 lz = Lz - new float4(ray.origin.z);
            float4 hz = Hz - new float4(ray.origin.z);
            float4 nearZt = lz * new float4(invD.z);
            float4 farZt = hz * new float4(invD.z);

            float4 nearX = math.min(nearXt, farXt);
            float4 farX = math.max(nearXt, farXt);

            float4 nearY = math.min(nearYt, farYt);
            float4 farY = math.max(nearYt, farYt);

            float4 nearZ = math.min(nearZt, farZt);
            float4 farZ = math.max(nearZt, farZt);

            float4 nearMax = math.max(math.max(math.max(nearX, nearY), nearZ), float4.zero);
            float4 farMin = math.min(math.min(farX, farY), farZ);

            return (nearMax <= farMin) & (lx <= hx);
        }
    }
 }