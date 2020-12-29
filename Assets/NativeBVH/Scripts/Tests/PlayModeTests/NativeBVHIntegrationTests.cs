using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NativeBVH.Editor;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace NativeBVH {
    public class NativeBVHIntegrationTests {

        struct MovingObject {
            public RigidTransform transform;
            public float3 velocity;
        }
        
        [UnityTest]
        public IEnumerator Test() {
            int amount = 3000;
            Random.InitState(0);
            var go = new GameObject("debug");
            go.AddComponent<NativeBVHDebugDrawer>();
            var world = new BVHTreeWorld(10000, Allocator.Persistent);
            
            var movingObjects = new List<MovingObject>();
            
            NativeBVHDebugDrawer.LastTree = world.tree;
            NativeList<Collider> colliders = new NativeList<Collider>(5000, Allocator.TempJob);

            for (int i = 0; i < amount/2; i++) {
                var center = new float3(UnityEngine.Random.Range(0, 0), UnityEngine.Random.Range(0, 0),  UnityEngine.Random.Range(0, 0));
                var size = new float3(UnityEngine.Random.Range(1, 3), UnityEngine.Random.Range(1, 3), UnityEngine.Random.Range(1, 3));
                colliders.Add(BoxCollider.Create(center, size));
            }
            
            for (int i = 0; i < amount/2; i++) {
                var center = new float3(UnityEngine.Random.Range(0, 0), UnityEngine.Random.Range(0, 0),  UnityEngine.Random.Range(0, 0));
                colliders.Add(SphereCollider.Create(center, UnityEngine.Random.Range(10, 15)));
            }
            
            // Warm-up
            new AddLeavesJob {
                Tree = world.tree,
                Leaves = new NativeList<Collider>(0, Allocator.TempJob)
            }.Run();
            
            new BVHTreeWorld.InsertJob {
                Tree = world.tree,
                Entities = world.entities,
                Leaves = colliders
            }.Run();

            
            var until = Time.time + 30;

            // Prep raycast
            var rayResult = new NativeList<int>(64, Allocator.TempJob);
            var rayVisited = new NativeList<int>(64, Allocator.TempJob);
            var ray = new NativeBVHTree.Ray {
                origin = new float3(-30, -30, 0),
                direction = math.normalize(new float3(5, 5, 5)),
                minDistance = 0,
                maxDistance = 500
            };
            // Job
            var job = new RaycastJob {
                Tree = world.tree,
                Ray = ray,
                Results = rayResult,
            };
            
            for (var i = 0; i < amount; i++) {
                movingObjects.Add(new MovingObject {
                    transform = RigidTransform.identity,
                    velocity = new float3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f))
                });
            }
            
            while (Time.time < until) {
                // Update
                Profiler.BeginSample("World.Update");
                world.Update();
                Profiler.EndSample();
                Profiler.BeginSample("Raycast");
                job.Run();
                Profiler.EndSample();
                yield return null;


                for (var i = 1; i < movingObjects.Count; i++) {
                    var movingObject = movingObjects[i];
                    movingObject.transform.pos += movingObject.velocity * Time.deltaTime * 2;
                    world.UpdateTransform(i, movingObject.transform);
                    movingObjects[i] = movingObject;
                }

                // Assertion
                //if (rayResult.Length != 9) {
                //    Debug.LogError("Expected 9 hits");
                //}
                
                // Debug
                NativeBVHDebugDrawer.LastTree = world.tree;
                NativeBVHDebugDrawer.LastTreeRayHits = rayResult.ToArray();
                NativeBVHDebugDrawer.LastTreeRayVisited = new bool[world.tree.DebugGetTotalNodesLength()+1];
                foreach (var i in rayVisited) {
                    NativeBVHDebugDrawer.LastTreeRayVisited[i] = true;
                }
                NativeBVHDebugDrawer.LastRay = ray;
            }
        }
        
         private void RunJob(int amount) {
            // Insertion
            NativeList<Collider> leaves = new NativeList<Collider>(amount, Allocator.TempJob);
            var tree = new NativeBVHTree(5000, Allocator.Persistent);
            for (int i = 0; i < amount / 2; i++) {
                var center = new float3(UnityEngine.Random.Range(0, 300), UnityEngine.Random.Range(0, 300),  UnityEngine.Random.Range(0, 300));
                var size = new float3(UnityEngine.Random.Range(5, 10), UnityEngine.Random.Range(5, 10), UnityEngine.Random.Range(5, 10));
                leaves.Add(BoxCollider.Create(center, size));
            }
            
            for (int i = 0; i < amount / 2; i++) {
                var center = new float3(UnityEngine.Random.Range(0, 300), UnityEngine.Random.Range(0, 300),  UnityEngine.Random.Range(0, 300));
                leaves.Add(SphereCollider.Create(center, UnityEngine.Random.Range(5, 10)));
            }
            
            
            var s = Stopwatch.StartNew();
            new AddLeavesJob {
                Tree = tree,
                Leaves = leaves
            }.Run();
            Debug.Log("Insertion took: " + s.Elapsed.TotalMilliseconds);
            leaves.Dispose();
            
            // Prep raycast
            var rayResult = new NativeList<int>(64, Allocator.TempJob);
            var rayVisited = new NativeList<int>(64, Allocator.TempJob);
            var ray = new NativeBVHTree.Ray {
                origin = new float3(-30, -30, -30),
                direction = math.normalize(new float3(5, 5, 5)),
                minDistance = 0,
                maxDistance = 500
            };
            
            // Job
            var job = new RaycastJob {
                Tree = tree,
                Ray = ray,
                Results = rayResult,
            };

            s.Restart();
            job.Run();
            Debug.Log("Took: " + s.Elapsed.TotalMilliseconds);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeRayHits = rayResult.ToArray();
            NativeBVHDebugDrawer.LastTreeRayVisited  = new bool[tree.DebugGetTotalNodesLength()];
            foreach (var i in rayVisited) {
                NativeBVHDebugDrawer.LastTreeRayVisited[i] = true;
            }
            NativeBVHDebugDrawer.LastRay = ray;
        }
    }
}