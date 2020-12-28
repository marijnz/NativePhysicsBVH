using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using RaycastHit = Unity.Physics.RaycastHit;

namespace NativeBVH {
    public class NativeBVHPerfTests {

        [Test]
        public void TestPerf() {
            UnityEngine.Random.InitState(0);

            TestPerf(1);
            TestPerf(1500);
        }

        public void TestPerf(int amount) {
            var world = new PhysicsWorld(amount, 0, 0);

            for (int i = 0; i < amount; i++) {
                var center = new float3(UnityEngine.Random.Range(0, 300), UnityEngine.Random.Range(0, 300),  UnityEngine.Random.Range(0, 300));
                var size = new float3(UnityEngine.Random.Range(5, 10), UnityEngine.Random.Range(5, 10), UnityEngine.Random.Range(5, 10));
                
                var col = Unity.Physics.BoxCollider.Create(new BoxGeometry() {
                    Center = default,
                    Size = size,
                    Orientation = quaternion.identity,
                    BevelRadius = 0.1f
                });
                
                var staticBodies = world.StaticBodies;
                var rb = staticBodies[i];
                rb.WorldFromBody.pos = center;
                rb.WorldFromBody.rot = quaternion.identity;
                rb.Collider = col;
                staticBodies[i] = rb;
            }
            var s = Stopwatch.StartNew();
            
            new BuildWorldJob { PhysicsWorld = world}.Run();
            Debug.Log("BuildBroadphase took: " + s.Elapsed.TotalMilliseconds);

            var rayResult = new NativeList<RaycastHit>(64, Allocator.TempJob);
            var rayJob = new RayJob {
                CollisionWorld = world.CollisionWorld,
                RayInput = new RaycastInput {
                    Start = new float3(-10, -10, 0),
                    End = new float3(500, 500, 500),
                    Filter = CollisionFilter.Default
                },
                Results = rayResult
            };

            s.Restart();
            rayJob.Run();
            Debug.Log("Took: " + s.Elapsed.TotalMilliseconds + " results: " + rayResult.Length);
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
                for (int i = 0; i < 10000; i++) {
                    Results.Clear();
                    CollisionWorld.CastRay(RayInput, ref Results);
                }
            }
        }
    }
}


