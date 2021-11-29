using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using RaycastHit = Unity.Physics.RaycastHit;

namespace NativeBVH {
    public class NativeBVHPerfTests {
        private bool enableLog = false;
        
        [UnityTest]
        public IEnumerator CreationAndRaycastPerformanceTest() {
            Random.InitState(0);

            // Warm-up
            DoTest();
            DoTest();
            enableLog = true;
            yield return null;
            DoTest();
            //while (true) {
            //    yield return null;
            //}
        }

        private void DoTest() {
            var amount = PerformanceComparisonConfig.ObjectCount;
            var world = new BVHTreeWorld(amount, Allocator.Persistent);

            Random.InitState(0);
            var colliders = new NativeArray<Collider>(amount, Allocator.TempJob);
            var transforms = new NativeArray<RigidTransform>(amount, Allocator.TempJob);
            for (int i = 0; i < PerformanceComparisonConfig.ObjectCount; i++) {
                colliders[i] = BoxCollider.Create(float3.zero, PerformanceComparisonConfig.GetRandomSize());
                transforms[i] = new RigidTransform(quaternion.identity, PerformanceComparisonConfig.GetRandomPosition());
            }
           
            
            var s = Stopwatch.StartNew();
            Profiler.BeginSample("broad");
            new BVHTreeWorld.InsertCollidersAndTransformsJob {
                Tree = world.tree,
                Bodies = world.bodies,
                Colliders = colliders,
                Transforms = transforms
            }.Run();
            Profiler.EndSample();
            if(enableLog) Debug.Log("Building broad phase took: " + s.Elapsed.TotalMilliseconds);
            
            s.Restart();
            var o = new NativeArray<AABB3D>(1, Allocator.TempJob);
            new BVHTreeWorld.CalculateJob() {
                Bodies = world.bodies,
                Output = o
            }.Run();
            if(enableLog) Debug.Log("calcjob  took: " + s.Elapsed.TotalMilliseconds);
            Debug.Log(o[0].LowerBound + " " + o[0].UpperBound);
        }

        [BurstCompile]
        public struct RayJob : IJob {
            [ReadOnly] 
            public NativeBVHTree Tree;
            [ReadOnly]
            public NativeBVHTree.Ray RayInput;
            [WriteOnly]
            public NativeList<int> Results;

            public void Execute() {
                for (int i = 0; i < PerformanceComparisonConfig.RaycastAmount; i++) {
                    Results.Clear();
                    Tree.RaycastQuery(RayInput, Results);
                }
            }
        }
    }
}


