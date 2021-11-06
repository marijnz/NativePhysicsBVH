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
using RaycastHit = Unity.Physics.RaycastHit;

namespace NativeBVH {
    public class UnityPhysicsPerformanceTest {
        private bool enableLog = false;
        
        [Test]
        public void CreationAndRaycastPerformanceTest() {
            UnityEngine.Random.InitState(0);
            
            // Warm-up
            DoTest();
            DoTest();
            enableLog = true;
            DoTest();
        }

        public void DoTest() {
            var amount = PerformanceComparisonConfig.ObjectCount;

            var world = new PhysicsWorld(0, amount, 0);
            
            UnityEngine.Random.InitState(0);
            for (int i = 0; i < amount; i++) {
                var col = Unity.Physics.BoxCollider.Create(new BoxGeometry() {
                    Center = default,
                    Size = PerformanceComparisonConfig.GetRandomSize(),
                    Orientation = quaternion.identity,
                    BevelRadius = 0
                });
                
                var bodies = world.DynamicBodies;
                var rb = bodies[i];
                rb.WorldFromBody.pos = PerformanceComparisonConfig.GetRandomPosition();
                rb.WorldFromBody.rot = quaternion.identity;
                rb.Collider = col;
                bodies[i] = rb;
            }
            
            var s = Stopwatch.StartNew();
            Profiler.BeginSample("broad");
            new BuildWorldJob {PhysicsWorld = world}.Run();
            Profiler.EndSample();
            if(enableLog) Debug.Log("Building broad phase took: " + s.Elapsed.TotalMilliseconds);

            var rayResult = new NativeList<RaycastHit>(64, Allocator.TempJob);
            var rayJob = new RayJob {
                CollisionWorld = world.CollisionWorld,
                RayInput = new RaycastInput {
                    Start = PerformanceComparisonConfig.RayStart,
                    End = PerformanceComparisonConfig.RayEnd,
                    Filter = CollisionFilter.Default
                },
                Results = rayResult
            };

            s.Restart();
            rayJob.Run();
            if(enableLog) Debug.Log("Raycasts took: " + s.Elapsed.TotalMilliseconds + " results: " + rayResult.Length);
            
            
            s.Restart();
            new BuildWorldJob {PhysicsWorld = world}.Run();
            if(enableLog) Debug.Log("Building broad phase again after no changes took: " + s.Elapsed.TotalMilliseconds);
        }

        [BurstCompile]
        public struct BuildWorldJob : IJob {
            public PhysicsWorld PhysicsWorld;
            public void Execute() {
                PhysicsWorld.CollisionWorld.BuildBroadphase(ref PhysicsWorld, 1, -9.81f * math.up());
            }
        }

        [BurstCompile]
        public struct RayJob : IJob {
            [ReadOnly] 
            public CollisionWorld CollisionWorld;
            [ReadOnly]
            public RaycastInput RayInput;
            [WriteOnly]
            public NativeList<RaycastHit> Results;

            public void Execute() {
                for (int i = 0; i < PerformanceComparisonConfig.RaycastAmount; i++) {
                    Results.Clear();
                    CollisionWorld.CastRay(RayInput, ref Results);
                }
            }
        }
    }
}


