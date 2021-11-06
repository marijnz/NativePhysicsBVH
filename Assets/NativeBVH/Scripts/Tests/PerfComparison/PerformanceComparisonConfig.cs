using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace NativeBVH {
    public static class PerformanceComparisonConfig {
        public const int ObjectCount = 1500;
        public const float RaycastAmount = 10000;

        public const float SpawnRange = 300;
        
        const float ObjectMinSize = 5;
        const float ObjectMaxSize = 10;

        public static float3 GetRandomPosition() => new float3(Random.Range(0, SpawnRange), Random.Range(0, SpawnRange),  Random.Range(0, SpawnRange));
        public static float3 GetRandomSize() => new float3(Random.Range(ObjectMinSize, ObjectMaxSize), Random.Range(ObjectMinSize, ObjectMaxSize), Random.Range(ObjectMinSize, ObjectMaxSize));
    }
}