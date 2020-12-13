using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace NativeBVH.Editor {
    public class NativeBVHTests {
        [Test]
        public void TestRandom() {
            // Random boxes in a 3d space
            var tree = new NativeBVHTree(Allocator.Temp);
            for (int i = 0; i < 20; i++) {
                var lower = new float3(UnityEngine.Random.Range(0, 30), UnityEngine.Random.Range(0, 30),  UnityEngine.Random.Range(0, 30));
                var upper = lower + new float3(UnityEngine.Random.Range(5, 20), UnityEngine.Random.Range(5, 20), UnityEngine.Random.Range(5, 20));
                tree.InsertLeaf(new AABB3D(lower, upper));
            }
            NativeBVHDebugDrawer.LastTree = tree;
        }
        
        [Test]
        public void TestForest() {
            // Create a "forest", many boxes with a y-position of 0
            var tree = new NativeBVHTree(Allocator.Temp);
            for (int i = 0; i < 50; i++) {
                var pos =  new float3(UnityEngine.Random.Range(0, 200), 0,UnityEngine.Random.Range(0, 200));
                tree.InsertLeaf(new AABB3D(pos, pos + new float3(1,2,1)));
            }
            NativeBVHDebugDrawer.LastTree = tree;
        }
    }
}


