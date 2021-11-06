using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using RaycastHit = Unity.Physics.RaycastHit;

namespace NativeBVH {
    public class NativeBVHPerfTests {
        private bool enableLog = false;
        
        [Test]
        public void CreationAndRaycastPerformanceTest() {
            Random.InitState(0);

            // Warm-up
            DoTest();
            DoTest();
            enableLog = true;
            DoTest();
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

            var rayResult = new NativeList<int>(64, Allocator.TempJob);

            var start = new float3(-10, -10, 0);
            var end = new float3(500, 500, 500);
            
            var rayJob = new RayJob {
                Tree = world.tree,
                RayInput = new NativeBVHTree.Ray {
                    origin = start,
                    direction = math.normalize(end-start),
                    maxDistance = math.distance(start, end),
                },
                Results = rayResult
            };

            s.Restart();
            rayJob.Run();
            if(enableLog) Debug.Log("Raycasts took: " + s.Elapsed.TotalMilliseconds + " results: " + rayResult.Length);
            
            s.Restart();
            world.Update();
            if(enableLog) Debug.Log("Building broad phase again after no changes took: " + s.Elapsed.TotalMilliseconds);
            
            s.Restart();
            for (int i = 0; i < 100; i++) {
                int randomIndex = Random.Range(1, PerformanceComparisonConfig.ObjectCount);
                world.UpdateTransform(randomIndex, new RigidTransform(quaternion.identity, PerformanceComparisonConfig.GetRandomPosition()));
            }
            world.Update();
            if(enableLog) Debug.Log("Building broad phase again after some changes took: " + s.Elapsed.TotalMilliseconds);
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


