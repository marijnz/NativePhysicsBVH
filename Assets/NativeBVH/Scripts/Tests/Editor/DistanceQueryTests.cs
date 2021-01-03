using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace NativeBVH.Editor {
    public class DistanceQueryTests {

        [SetUp]
        public void Setup() {
            UnityEngine.Random.InitState(0);

            NativeBVHDebugDrawer.LastTree = default;
            NativeBVHDebugDrawer.LastTreeHits = default;
            NativeBVHDebugDrawer.LastTreeRayVisited  = default;
            NativeBVHDebugDrawer.LastRay = default;
        }

        [Test]
        public void TestDistanceTwoBoxes() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            int expectedIndex = tree.InsertLeaf(BoxCollider.Create(new float3(0, 1, 0), new float3(2, 2, 2)));
            tree.InsertLeaf(BoxCollider.Create(new float3(0, 1.2f, 0), new float3(2, 2, 2)));
            
            // Distance query
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var query = new NativeBVHTree.DistanceQueryInput {
                origin = new float3(0, -1, 0),
                maxDistance = 1.1f
            };
            tree.DistanceQuery(query, rayResult);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeHits = rayResult.ToArray();
            
            // Assert
            Assert.AreEqual(1, rayResult.Length, "Expected only one hit");
            Assert.AreEqual(expectedIndex, rayResult[0], "Expected nearer box to be the result");
        }
        
        [Test]
        public void TestDistanceTwoSpheres() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            int expectedIndex = tree.InsertLeaf(SphereCollider.Create(new float3(0, 2, 0), 2));
            tree.InsertLeaf(SphereCollider.Create(new float3(0, 5, 0), 2));
            
            // Distance query
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var query = new NativeBVHTree.DistanceQueryInput {
                origin = new float3(0, -1, 0),
                maxDistance = 3f
            };
            tree.DistanceQuery(query, rayResult);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeHits = rayResult.ToArray();
            
            // Assert
            Assert.AreEqual(1, rayResult.Length, "Expected only one hit");
            Assert.AreEqual(expectedIndex, rayResult[0], "Expected nearer sphere to be the result");
        }
        
        [Test]
        public void TestDistanceTwoBoxesWithTransformPosition() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            var transform = RigidTransform.identity;
            transform.pos = new float3(10, 1, 0);
            var expectedIndex = tree.InsertLeaf(BoxCollider.Create(new float3(-10, 0, 0), new float3(2, 2, 2)), transform);
            tree.InsertLeaf(BoxCollider.Create(new float3(-10, 0, 0), new float3(2, 2, 2)));
            
            // Distance query
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var query = new NativeBVHTree.DistanceQueryInput {
                origin = new float3(0, -1, 0),
                maxDistance = 1.1f
            };
            tree.DistanceQuery(query, rayResult);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeHits = rayResult.ToArray();
            
            // Assert
            Assert.AreEqual(1, rayResult.Length, "Expected only one hit");
            Assert.AreEqual(expectedIndex, rayResult[0], "Expected nearer box to be the result");
        }
        
        [Test]
        public void TestDistanceTwoBoxesWithTransformRotationAroundFarPivot() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            var transform = RigidTransform.identity;
            transform.rot = quaternion.Euler(0, 0, 180);
            var expectedIndex = tree.InsertLeaf(BoxCollider.Create(new float3(-10, 0, 0), new float3(2, 2, 2)), transform);
            tree.InsertLeaf(BoxCollider.Create(new float3(-10, 0, 0), new float3(2, 2, 2)));
            
            // Distance query
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var query = new NativeBVHTree.DistanceQueryInput {
                origin = new float3(7, 7, 0),
                maxDistance = 2f
            };
            tree.DistanceQuery(query, rayResult);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeHits = rayResult.ToArray();
            
            // Assert
            Assert.AreEqual(1, rayResult.Length, "Expected only one hit");
            Assert.AreEqual(expectedIndex, rayResult[0], "Expected rotated box (to top-right) to be the hit");
        }
        
        [Test]
        public void TestDistanceTwoBoxesWithTransformRotationAroundSelf() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            var transform = RigidTransform.identity;
            transform.pos = new float3(1, 1, 0);
            transform.rot = quaternion.Euler(0, 0, 180);
            tree.InsertLeaf(BoxCollider.Create(new float3(0, 0, 0), new float3(2, 2, 2)), transform);
            var secondTransform = RigidTransform.identity;
            secondTransform.pos = new float3(1, 1, 0);
            var expectedIndex = tree.InsertLeaf(BoxCollider.Create(new float3(0, 0, 0), new float3(2, 2, 2)), secondTransform);
            
            // Distance query
            var rayResult = new NativeList<int>(64, Allocator.Temp);
            var query = new NativeBVHTree.DistanceQueryInput {
                origin = new float3(0, 0, 0),
                maxDistance = 0.1f
            };
            tree.DistanceQuery(query, rayResult);
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            NativeBVHDebugDrawer.LastTreeHits = rayResult.ToArray();
            
            // Assert
            Assert.AreEqual(1, rayResult.Length, "Expected only one hit");
            Assert.AreEqual(expectedIndex, rayResult[0], "Expected non-rotated box to be the hit");
        }
    }
}


