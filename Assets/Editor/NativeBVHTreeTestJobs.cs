using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace NativeBVH {
	[BurstCompile]
	public struct AddLeavesJob : IJob {
		public NativeBVHTree Tree;
		[ReadOnly] public NativeArray<Collider> Leaves;

		public void Execute() {
			for (var i = 0; i < Leaves.Length; i++) {
				Tree.InsertLeaf(Leaves[i]);
			}
		}
	}

	[BurstCompile]
	public struct RaycastJob : IJob {
		public NativeBVHTree Tree;
		[ReadOnly] public NativeBVHTree.Ray Ray;
		public NativeList<int> Results;

		public void Execute() {
			for (int i = 0; i < 10000; i++) {
				Results.Clear();
				Tree.RayCast(Ray, Results);
			}
		}
	}
}
