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
        
        [UnityTest]
        public IEnumerator Test() {
            Random.InitState(0);
            var go = new GameObject("debug");
            go.AddComponent<NativeBVHDebugDrawer>();
            var world = new BVHTreeDynamicWorld(5000, Allocator.Persistent);
            
            NativeBVHDebugDrawer.LastTree = world.tree;

            for (int i = 0; i < 500; i++) {
                var center = new float3(UnityEngine.Random.Range(0, 300), UnityEngine.Random.Range(0, 300),  UnityEngine.Random.Range(0, 300));
                var size = new float3(UnityEngine.Random.Range(10, 15), UnityEngine.Random.Range(10, 15), UnityEngine.Random.Range(10, 15));
                world.Add(BoxCollider.Create(center, size));
            }
            
            for (int i = 0; i < 500; i++) {
                var center = new float3(UnityEngine.Random.Range(0, 300), UnityEngine.Random.Range(0, 300),  UnityEngine.Random.Range(0, 300));
                world.Add(SphereCollider.Create(center, UnityEngine.Random.Range(10, 15)));
            }

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
            
            
            while (Time.time < until) {
                // Update
                Profiler.BeginSample("World.Update");
                new BVHTreeDynamicWorld.UpdateJob {World = world}.Run();
                Profiler.EndSample();
                Profiler.BeginSample("Raycast");
                job.Run();
                Profiler.EndSample();
                yield return null;
                
                // Assertion
                if (rayResult.Length != 9) {
                    Debug.LogError("Expected 9 hits");
                }
                
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