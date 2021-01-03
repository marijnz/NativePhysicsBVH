using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace NativeBVH.Editor {
    public class BasicCreationTests {

        [SetUp]
        public void Setup() {
            Random.InitState(0);

            NativeBVHDebugDrawer.LastTree = default;
            NativeBVHDebugDrawer.LastTreeHits = default;
            NativeBVHDebugDrawer.LastTreeRayVisited  = default;
            NativeBVHDebugDrawer.LastRay = default;
        }
        
        [Test]
        public void TestCreateBoxCollider() {
            var collider = BoxCollider.Create(new float3(1, 1, 1), new float3(2, 2, 2));
            collider.RayQuery(new NativeBVHTree.Ray {direction = new float3(2, 2, 2)});
        }
        
        [Test]
        public void TestManyBoxes() {
            // Random boxes in a 3d space
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            for (int i = 0; i < 20; i++) {
                var lower = new float3(Random.Range(0, 30), Random.Range(0, 30),  Random.Range(0, 30));
                var upper = lower + new float3(Random.Range(5, 20), Random.Range(5, 20), Random.Range(5, 20));
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
                var pos =  new float3(Random.Range(0, 200), 0,Random.Range(0, 200));
                tree.InsertLeaf(BoxCollider.Create(pos, pos + new float3(1,2,1)));
            }
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
        }
    }
}


