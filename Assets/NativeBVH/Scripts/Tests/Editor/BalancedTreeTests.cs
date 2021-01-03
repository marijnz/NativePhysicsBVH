using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace NativeBVH.Editor {
    public class BalancedTreeTests {

        [SetUp]
        public void Setup() {
            Random.InitState(0);

            NativeBVHDebugDrawer.LastTree = default;
            NativeBVHDebugDrawer.LastTreeHits = default;
            NativeBVHDebugDrawer.LastTreeRayVisited  = default;
            NativeBVHDebugDrawer.LastRay = default;
        }
        
        [Test]
        public void TestBalancedTreeTwoClusters() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            // Left cluster
            tree.InsertLeaf(BoxCollider.Create(new float3(-20, 5, 0), new float3(1, 1, 1)));
            tree.InsertLeaf(BoxCollider.Create(new float3(-21, 3, 0), new float3(1, 1, 1)));
            tree.InsertLeaf(BoxCollider.Create(new float3(-25, 3, 0), new float3(1, 1, 1)));
            
            // Right cluster
            tree.InsertLeaf(BoxCollider.Create(new float3(19, 3, 0), new float3(1, 1, 1)));
            tree.InsertLeaf(BoxCollider.Create(new float3(21, 5, 0), new float3(1, 1, 1)));
            tree.InsertLeaf(BoxCollider.Create(new float3(22, 1, 0), new float3(1, 1, 1)));
            
            tree.InsertLeaf(BoxCollider.Create(new float3(0, 1, 0), new float3(1, 1, 1)));
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            
            // Assert
            var surfaceArea = GetTotalSurfaceArea(ref tree, true);
            Assert.LessOrEqual(surfaceArea, 164, "Expected surface area to be lower (this can be increased if there's a good reason for it)");
        }
        
        [Test]
        public void TestBalancedTreeMultipleClustersWithRemoval() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            // Left cluster
            for (int i = 0; i < 20; i++) {
                tree.InsertLeaf(BoxCollider.Create(new float3(Random.Range(-30, -33), Random.Range(-3,3), Random.Range(-3,3)), new float3(1, 1, 1)));
            }
            // Right cluster
            for (int i = 0; i < 20; i++) {
                tree.InsertLeaf(BoxCollider.Create(new float3(Random.Range(30, 33), Random.Range(-3,3), Random.Range(-3,3)), new float3(1, 1, 1)));
            }
            
            // Forward cluster
            for (int i = 0; i < 20; i++) {
                tree.InsertLeaf(BoxCollider.Create(new float3(Random.Range(-3, 3), Random.Range(-3,3), Random.Range(-30,-33)), new float3(1, 1, 1)));
            }
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;

            // Assert
            using (var indices = new NativeList<int>(Allocator.Temp)) {
                for (int i = 0; i < 10; i++) {
                    var removed = new List<Leaf>();
                    
                    // Remove a random amount of leaves
                    for (int j = 0; j < Random.Range(5, 30); j++) {
                        indices.Clear();
                        tree.DebugGetAllChildren(tree.DebugGetRootNodeIndex(), indices, true);
                        var randomIndex = Random.Range(0, indices.Length);
                        var index = indices[randomIndex];
                        var leaf = tree.DebugGetNode(index).leaf;
                        removed.Add(leaf);
                        tree.RemoveLeaf(index);
                    }
                    
                    // Insert them back in again
                    foreach (var leaf in removed) {
                        tree.InsertLeaf(leaf);
                    }
                    
                    // And ensure the surface area didn't increase
                    var surfaceArea = GetTotalSurfaceArea(ref tree, true);
                    Assert.LessOrEqual(surfaceArea, 3650, "Expected surface area to be lower (this can be increased if there's a good reason for it)");
                    Debug.Log(surfaceArea);
                }
            }
        }
        
        [Test]
        public void TestBalancedTreeSortedInput() {
            // Insertion
            var tree = new NativeBVHTree(64, Allocator.Persistent);
            // Left cluster
            for (int i = 0; i < 20; i++) {
                tree.InsertLeaf(BoxCollider.Create(new float3(3 * i, i * 3, 0), new float3(1, 1, 1)));
            }
            
            // Debug
            NativeBVHDebugDrawer.LastTree = tree;
            
            // Assert
            var surfaceArea = GetTotalSurfaceArea(ref tree, true);
            Assert.LessOrEqual(surfaceArea, 5250, "Expected surface area to be lower (this can be increased if there's a good reason for it)");
            Debug.Log(surfaceArea);
        }

        private float GetTotalSurfaceArea(ref NativeBVHTree tree, bool excludeRoot = false) {
            using (var indices = new NativeList<int>(Allocator.Temp)) {
                tree.DebugGetAllChildren(tree.DebugGetRootNodeIndex(), indices);
                var totalSurfaceArea = 0f;
                for (int i = 0; i < indices.Length; i++) {
                    var node = tree.DebugGetNode(i);
                    if ((!excludeRoot || i != tree.DebugGetRootNodeIndex()) && !node.isLeaf) {
                        totalSurfaceArea += node.box.Area();
                    }
                }
                return totalSurfaceArea;
            }
        }
    }
}


