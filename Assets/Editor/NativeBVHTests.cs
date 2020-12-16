using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

namespace NativeBVH.Editor {
    public class NativeBVHTests {

        [SetUp]
        public void Setup() {
            UnityEngine.Random.InitState(0);
        }

        [Test]
        public void TestManyBoxes() {
            // Random boxes in a 3d space
            var tree = new NativeBVHTree(64);
            for (int i = 0; i < 20; i++) {
                var lower = new float3(UnityEngine.Random.Range(0, 30), UnityEngine.Random.Range(0, 30),  UnityEngine.Random.Range(0, 30));
                var upper = lower + new float3(UnityEngine.Random.Range(5, 20), UnityEngine.Random.Range(5, 20), UnityEngine.Random.Range(5, 20));
                tree.InsertLeaf(new AABB3D(lower, upper));
            }
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
        }
        
        [Test]
        public void TestForest() {
            // Create a "forest", many boxes with a y-position of 0
            var tree = new NativeBVHTree(64);
            for (int i = 0; i < 50; i++) {
                var pos =  new float3(UnityEngine.Random.Range(0, 200), 0,UnityEngine.Random.Range(0, 200));
                tree.InsertLeaf(new AABB3D(pos, pos + new float3(1,2,1)));
            }
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
        }
        
        [Test]
        public void TestRayTwoBoxes() {
            // Insertion
            var tree = new NativeBVHTree(64);
            tree.InsertLeaf(new AABB3D(float3.zero, new float3(2, 2, 2)));
            tree.InsertLeaf(new AABB3D(new float3(4, 4, 4), new float3(6, 6, 6)));
            
            // Raycast
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var ray = new NativeBVHTree.Ray {
                Origin = new float3(-1, 1, 0),
                Direction = new float3(10, 0, 10),
                MinDistance = 0,
                MaxDistance = 20
            };
            tree.RayCast(ray, rayResult);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeRayHits = rayResult.ToArray();
            NativeBVHDebugDrawer.LastRay = ray;
        }
        
        [Test]
        public void TestRayRandomManyBoxes() {
            // Insertion
            var tree = new NativeBVHTree(64);
            for (int i = 0; i < 50; i++) {
                var lower = new float3(UnityEngine.Random.Range(0, 50), UnityEngine.Random.Range(0, 50),  UnityEngine.Random.Range(0, 50));
                var upper = lower + new float3(UnityEngine.Random.Range(2, 5), UnityEngine.Random.Range(2, 5), UnityEngine.Random.Range(2, 5));
                tree.InsertLeaf(new AABB3D(lower, upper));
            }
            
            // Raycast
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var ray = new NativeBVHTree.Ray {
                Origin = new float3(-10, -10, 0),
                Direction = new float3(50, 50, 50),
                MinDistance = 0,
                MaxDistance = 200
            };
            tree.RayCast(ray, rayResult);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeRayHits = rayResult.ToArray();
            NativeBVHDebugDrawer.LastRay = ray;
        }
        
        [Test]
        public void TestJobRayRandomManyBoxes() {
            // TODO: To fix -  this one misses a raycast hit
            RunJob(1024);
        }

        private void RunJob(int amount) {
            // Insertion
            NativeList<AABB3D> leaves = new NativeList<AABB3D>(1024, Allocator.TempJob);
            var tree = new NativeBVHTree(amount, Allocator.Persistent);
            for (int i = 0; i < amount; i++) {
                var lower = new float3(UnityEngine.Random.Range(0, 300), UnityEngine.Random.Range(0, 300),  UnityEngine.Random.Range(0, 300));
                var upper = lower + new float3(UnityEngine.Random.Range(2, 5), UnityEngine.Random.Range(2, 5), UnityEngine.Random.Range(2, 5));
                leaves.Add(new AABB3D(lower, upper));
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
            var ray = new NativeBVHTree.Ray {
                Origin = new float3(-10, -10, 0),
                Direction = new float3(50, 50, 50),
                MinDistance = 0,
                MaxDistance = 500
            };
            
            // Job
            var job = new RaycastJob {
                Tree = tree,
                Ray = ray,
                Results = rayResult
            };

            s.Restart();
            job.Run();
            Debug.Log("Took: " + s.Elapsed.TotalMilliseconds);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeRayHits = rayResult.ToArray();
            NativeBVHDebugDrawer.LastRay = ray;
        }
    }
}


