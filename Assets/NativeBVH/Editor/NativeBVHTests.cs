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
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            for (int i = 0; i < 20; i++) {
                var lower = new float3(UnityEngine.Random.Range(0, 30), UnityEngine.Random.Range(0, 30),  UnityEngine.Random.Range(0, 30));
                var upper = lower + new float3(UnityEngine.Random.Range(5, 20), UnityEngine.Random.Range(5, 20), UnityEngine.Random.Range(5, 20));
                tree.InsertLeaf(BoxCollider.Create(lower, upper));
            }
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
        }
        
        [Test]
        public void TestForest() {
            // Create a "forest", many boxes with a y-position of 0
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            for (int i = 0; i < 50; i++) {
                var pos =  new float3(UnityEngine.Random.Range(0, 200), 0,UnityEngine.Random.Range(0, 200));
                tree.InsertLeaf(BoxCollider.Create(pos, pos + new float3(1,2,1)));
            }
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
        }
        
        [Test]
        public void TestRayTwoBoxes() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            tree.InsertLeaf(BoxCollider.Create(new float3(1, 1, 1), new float3(1, 1, 2)));
            tree.InsertLeaf(BoxCollider.Create(new float3(4, 4, 4), new float3(3, 3, 3)));
            
            // Raycast
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var ray = new NativeBVHTree.Ray {
                origin = new float3(-1, 1, 0),
                direction = new float3(10, 0, 10),
                minDistance = 0,
                maxDistance = 20
            };
            tree.RayCast(ray, rayResult);
            
            // Assert
            Assert.AreEqual(1, rayResult.Length, "Expected only one hit");
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeRayHits = rayResult.ToArray();
            NativeBVHDebugDrawer.LastRay = ray;
        }
        
        [Test]
        public void TestRayTwoSpheres() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            tree.InsertLeaf(SphereCollider.Create(new float3(1, 1, 1), 1));
            tree.InsertLeaf(SphereCollider.Create(new float3(4, 3, 6), 2.1f));
            
            // Raycast
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var ray = new NativeBVHTree.Ray {
                origin = new float3(-1, 1, 0),
                direction = new float3(10, 0, 10),
                minDistance = 0,
                maxDistance = 20
            };
            tree.RayCast(ray, rayResult);

            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeRayHits = rayResult.ToArray();
            NativeBVHDebugDrawer.LastRay = ray;
            
            // Assert
            Assert.AreEqual(1, rayResult.Length, "Expected only one hit");
        }
        
        [Test]
        public void TestRayThreeBoxesWithDeletion() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            var index = tree.InsertLeaf(BoxCollider.Create(new float3(1, 1, 1), new float3(1, 1, 2)));
            tree.InsertLeaf(BoxCollider.Create(new float3(4, 4, 4), new float3(3, 3, 3)));
            tree.InsertLeaf(BoxCollider.Create(new float3(8, 3, 4), new float3(2, 2, 2)));
            tree.RemoveLeaf(index);
            tree.InsertLeaf(BoxCollider.Create(new float3(1, 1, 1), new float3(1, 2, 2)));
            
            // Raycast
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var ray = new NativeBVHTree.Ray {
                origin = new float3(-1, 1, 0),
                direction = new float3(10, 0, 10),
                minDistance = 0,
                maxDistance = 20
            };
            tree.RayCast(ray, rayResult);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeRayHits = rayResult.ToArray();
            NativeBVHDebugDrawer.LastRay = ray;
            
            // Assert
            Assert.AreEqual(1, rayResult.Length, "Expected only one hit");
        }
        
        [Test]
        public void TestRayRandomManyBoxes() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            for (int i = 0; i < 50; i++) {
                var center = new float3(UnityEngine.Random.Range(0, 50), UnityEngine.Random.Range(0, 50),  UnityEngine.Random.Range(0, 50));
                var size = new float3(UnityEngine.Random.Range(2, 5), UnityEngine.Random.Range(2, 5), UnityEngine.Random.Range(2, 5));
                tree.InsertLeaf(BoxCollider.Create(center, size));
            } 
            
            // Raycast
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var ray = new NativeBVHTree.Ray {
                origin = new float3(-10, -10, 0),
                direction = new float3(50, 50, 50),
                minDistance = 0,
                maxDistance = 200
            };
            tree.RayCast(ray, rayResult);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeRayHits = rayResult.ToArray();
            NativeBVHDebugDrawer.LastRay = ray;
        }
        
        [Test]
        public void TestRayTwoBoxesWithLayers() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);

            uint layer1 = 1;
            uint layer2 = 1 << 1;
            tree.InsertLeaf(BoxCollider.Create(new float3(1, 1, 1), new float3(3, 3, 3)), layer1);
            tree.InsertLeaf(BoxCollider.Create(new float3(5, 1, 5), new float3(3, 3, 3)), layer2);
            
            // Raycast
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var ray = new NativeBVHTree.Ray {
                origin = new float3(-1, 1, 0),
                direction = new float3(10, 0, 10),
                minDistance = 0,
                maxDistance = 20,
                layerMask = ~layer1
            };
            tree.RayCast(ray, rayResult);

            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeRayHits = rayResult.ToArray();
            NativeBVHDebugDrawer.LastRay = ray;
            
            // Assert
            Assert.AreEqual(1, rayResult.Length, "Expected only one hit because of layer mask");
        }
        
        [Test]
        public void TestJobRayRandomManyBoxesAndSpheres() {
            // pre-warm
            RunJob(1);
            // run
            RunJob(15000);
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
                origin = new float3(-10, -10, 0),
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
        
        [Test]
        public void TestCreateBoxCollider() {
            var collider = BoxCollider.Create(new float3(1, 1, 1), new float3(2, 2, 2));
            collider.CastRay(new NativeBVHTree.Ray {direction = new float3(2, 2, 2)});
        }
    }
}


