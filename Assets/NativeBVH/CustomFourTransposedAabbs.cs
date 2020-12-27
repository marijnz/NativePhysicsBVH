 using Unity.Mathematics;
 using Unity.Physics;

 namespace NativeBVH {
     
    public struct CustomFourTransposedAabbs
    {
        public float4 Lx, Hx;    // Lower and upper bounds along the X axis.
        public float4 Ly, Hy;    // Lower and upper bounds along the Y axis.
        public float4 Lz, Hz;    // Lower and upper bounds along the Z axis.

        public static CustomFourTransposedAabbs Empty => new CustomFourTransposedAabbs
        {
            Lx = new float4(float.MaxValue),
            Hx = new float4(float.MinValue),
            Ly = new float4(float.MaxValue),
            Hy = new float4(float.MinValue),
            Lz = new float4(float.MaxValue),
            Hz = new float4(float.MinValue)
        };

        public void SetAllAabbs(AABB3D aabb)
        {
            Lx = new float4(aabb.LowerBound.x);
            Ly = new float4(aabb.LowerBound.y);
            Lz = new float4(aabb.LowerBound.z);
            Hx = new float4(aabb.UpperBound.x);
            Hy = new float4(aabb.UpperBound.y);
            Hz = new float4(aabb.UpperBound.z);
        }

        public void SetAabb(int index, AABB3D aabb)
        {
            Lx[index] = aabb.LowerBound.x;
            Hx[index] = aabb.UpperBound.x;

            Ly[index] = aabb.LowerBound.y;
            Hy[index] = aabb.UpperBound.y;

            Lz[index] = aabb.LowerBound.z;
            Hz[index] = aabb.UpperBound.z;
        }

        public AABB3D GetAabb(int index) => new AABB3D
        {
            LowerBound = new float3(Lx[index], Ly[index], Lz[index]),
            UpperBound = new float3(Hx[index], Hy[index], Hz[index])
        };

        public CustomFourTransposedAabbs GetAabbT(int index) => new CustomFourTransposedAabbs
        {
            Lx = new float4(Lx[index]),
            Ly = new float4(Ly[index]),
            Lz = new float4(Lz[index]),
            Hx = new float4(Hx[index]),
            Hy = new float4(Hy[index]),
            Hz = new float4(Hz[index])
        };

        public AABB3D GetCompoundAabb() => new AABB3D
        {
            LowerBound = new float3(math.cmin(Lx), math.cmin(Ly), math.cmin(Lz)),
            UpperBound = new float3(math.cmax(Hx), math.cmax(Hy), math.cmax(Hz))
        };

        public bool4 Overlap1Vs4(ref CustomFourTransposedAabbs aabbT)
        {
            bool4 lc = (aabbT.Lx <= Hx) & (aabbT.Ly <= Hy) & (aabbT.Lz <= Hz);
            bool4 hc = (aabbT.Hx >= Lx) & (aabbT.Hy >= Ly) & (aabbT.Hz >= Lz);
            bool4 c = lc & hc;
            return c;
        }

        public bool4 Overlap1Vs4(ref CustomFourTransposedAabbs other, int index)
        {
            CustomFourTransposedAabbs aabbT = other.GetAabbT(index);
            return Overlap1Vs4(ref aabbT);
        }

        public float4 DistanceFromPointSquared(ref Math.FourTransposedPoints tranposedPoint)
        {
            float4 px = math.max(tranposedPoint.X, Lx);
            px = math.min(px, Hx) - tranposedPoint.X;

            float4 py = math.max(tranposedPoint.Y, Ly);
            py = math.min(py, Hy) - tranposedPoint.Y;

            float4 pz = math.max(tranposedPoint.Z, Lz);
            pz = math.min(pz, Hz) - tranposedPoint.Z;

            return px * px + py * py + pz * pz;
        }

        public float4 DistanceFromPointSquared(ref Math.FourTransposedPoints tranposedPoint, float3 scale)
        {
            float4 px = math.max(tranposedPoint.X, Lx);
            px = (math.min(px, Hx) - tranposedPoint.X) * scale.x;

            float4 py = math.max(tranposedPoint.Y, Ly);
            py = (math.min(py, Hy) - tranposedPoint.Y) * scale.y;

            float4 pz = math.max(tranposedPoint.Z, Lz);
            pz = (math.min(pz, Hz) - tranposedPoint.Z) * scale.z;

            return px * px + py * py + pz * pz;
        }

        public float4 DistanceFromAabbSquared(ref CustomFourTransposedAabbs tranposedAabb)
        {
            float4 px = math.max(float4.zero, tranposedAabb.Lx - Hx);
            px = math.min(px, tranposedAabb.Hx - Lx);

            float4 py = math.max(float4.zero, tranposedAabb.Ly - Hy);
            py = math.min(py, tranposedAabb.Hy - Ly);

            float4 pz = math.max(float4.zero, tranposedAabb.Lz - Hz);
            pz = math.min(pz, tranposedAabb.Hz - Lz);

            return px * px + py * py + pz * pz;
        }

        public float4 DistanceFromAabbSquared(ref CustomFourTransposedAabbs tranposedAabb, float3 scale)
        {
            float4 px = math.max(float4.zero, tranposedAabb.Lx - Hx);
            px = math.min(px, tranposedAabb.Hx - Lx) * scale.x;

            float4 py = math.max(float4.zero, tranposedAabb.Ly - Hy);
            py = math.min(py, tranposedAabb.Hy - Ly) * scale.y;

            float4 pz = math.max(float4.zero, tranposedAabb.Lz - Hz);
            pz = math.min(pz, tranposedAabb.Hz - Lz) * scale.z;

            return px * px + py * py + pz * pz;
        }

        public bool4 Raycast(NativeBVHTree.Ray ray, float maxFraction, out float4 fractions, float3 invD)
        {
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
            float4 farMin = math.min(math.min(math.min(farX, farY), farZ), new float4(maxFraction));

            fractions = nearMax;

            return (nearMax <= farMin) & (lx <= hx);
        }
        
        public CustomFourTransposedAabbs UnionToTransposed(AABB3D other) {
            //TODO: select based on valid aabbs?
            return new CustomFourTransposedAabbs {
                Lx = math.min(Lx, other.LowerBound.x),
                Hx = math.max(Hx, other.UpperBound.x),
                Ly = math.min(Ly, other.LowerBound.y),
                Hy = math.max(Hy, other.UpperBound.y),
                Lz = math.min(Lz, other.LowerBound.z),
                Hz = math.max(Hz, other.UpperBound.z),
            };
        }
        
        public AABB3D UnionToAABB(AABB3D other) {
            var minX = math.min(Lx, other.LowerBound.x);
            var maxX = math.max(Hx, other.LowerBound.x);
            
            var minY = math.min(Ly, other.LowerBound.y);
            var maxY = math.max(Hy, other.UpperBound.y);
            
            var minZ = math.min(Lz, other.LowerBound.z);
            var maxZ = math.max(Hz, other.UpperBound.z);

            return new AABB3D {
                LowerBound = new float3(math.cmin(minX), math.cmin(minY), math.cmin(minZ)),
                UpperBound = new float3(math.cmax(maxX), math.cmax(maxY), math.cmax(maxZ))
            };
        }
        
        public float4 Areas() {
            var diffX = Hx - Lx;
            var diffY = Hy - Ly;
            var diffZ = Hz - Lz;
            return 2.0f * (diffX * diffY + diffY * diffZ + diffZ * diffX);
        }
        
        public float Area() {
            return math.csum(Areas());
        }
    }
 }