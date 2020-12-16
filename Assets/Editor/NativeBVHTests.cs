using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

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
    }
}


