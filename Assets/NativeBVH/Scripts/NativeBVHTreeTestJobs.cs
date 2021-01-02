using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace NativeBVH {
    [BurstCompile]
    public struct AddLeavesJob : IJob {
        public NativeBVHTree Tree;
        [ReadOnly] public NativeArray<Leaf> Leaves;

        public void Execute() {
            for (var i = 0; i < Leaves.Length; i++) {
                Tree.InsertLeaf(Leaves[i]);
            }
        }
    }
    
    [BurstCompile]
    public struct RemoveLeavesJob : IJob {
        public NativeBVHTree Tree;
        [ReadOnly] public NativeArray<int> Leaves;

        public void Execute() {
            for (var i = 0; i < Leaves.Length; i++) {
                Tree.RemoveLeaf(Leaves[i]);
            }
        }
    }

    [BurstCompile]
    public struct RaycastJob : IJob {
        [ReadOnly] public NativeBVHTree Tree;
        [ReadOnly] public NativeBVHTree.Ray Ray;
        public NativeList<int> Results;
        
        public void Execute() {
            for (int i = 0; i < 10000; i++) {
                Results.Clear();
                Tree.RaycastQuery(Ray, Results);
            }
        }
    }
    
    [BurstCompile]
    public struct DistanceJob : IJob {
        [ReadOnly] public NativeBVHTree Tree;
        [ReadOnly] public NativeBVHTree.Ray Ray;
        public NativeList<int> Results;
        
        public void Execute() {
            for (int i = 0; i < 10000; i++) {
                Results.Clear();
                Tree.RaycastQuery(Ray, Results);
            }
        }
    }
}
