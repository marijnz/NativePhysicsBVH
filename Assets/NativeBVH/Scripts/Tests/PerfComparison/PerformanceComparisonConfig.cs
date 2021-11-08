using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace NativeBVH {
    public static class PerformanceComparisonConfig {
        public const int ObjectCount = 1500;
        public const int RaycastAmount = 1000;

        public const float SpawnRange = 300;
        
        public static readonly float3 RayStart = new float3(-10, -10, 0);
        public static readonly float3 RayEnd = new float3(500, 500, 500);
        
        const float ObjectMinSize = 5;
        const float ObjectMaxSize = 10;

        public static float3 GetRandomPosition() => new float3(Random.Range(0, SpawnRange), Random.Range(0, SpawnRange),  Random.Range(0, SpawnRange));
        public static float3 GetRandomSize() => new float3(Random.Range(ObjectMinSize, ObjectMaxSize), Random.Range(ObjectMinSize, ObjectMaxSize), Random.Range(ObjectMinSize, ObjectMaxSize));
    }
}