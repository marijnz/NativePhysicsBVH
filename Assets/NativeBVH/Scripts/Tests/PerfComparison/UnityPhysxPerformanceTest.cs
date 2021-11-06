using System.Collections;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace NativeBVH {
    public class UnityPhysxPerformanceTest {
        [UnityTest]
        public IEnumerator Test() {
            var amount = PerformanceComparisonConfig.ObjectCount;
            
            Random.InitState(0);
            var s = Stopwatch.StartNew();
            for (int i = 0; i < amount; i++) {
                var collider = new GameObject().AddComponent<UnityEngine.BoxCollider>();
                collider.transform.position = PerformanceComparisonConfig.GetRandomPosition();
                collider.size = PerformanceComparisonConfig.GetRandomSize();
            }

            Debug.Log("Creating go's took (but can't measure broad phase construction): " + s.Elapsed.TotalMilliseconds);

            yield return null;

            var raycastAmount = PerformanceComparisonConfig.RaycastAmount;
            RaycastHit[] results = new RaycastHit[amount];
            int hits = 0;
            s.Restart();
            for (int i = 0; i < raycastAmount; i++) {
                var d = PerformanceComparisonConfig.RayEnd - PerformanceComparisonConfig.RayStart;
                hits = Physics.RaycastNonAlloc(PerformanceComparisonConfig.RayStart, math.normalize(d), results, math.length(d));
            }
            
            Debug.Log("Raycasts took: " + s.Elapsed.TotalMilliseconds + " results: " + hits);
        }
    }
}