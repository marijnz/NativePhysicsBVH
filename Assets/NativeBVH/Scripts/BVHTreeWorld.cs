using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace NativeBVH {
    public struct BVHTreeWorld : IDisposable {
        private const float ExpandSize = 1;

        public struct Body {
            public Collider collider;
            public RigidTransform transform;
            public int nodeId;
            public AABB3D expandedBounds;
        }

        public NativeList<Body> bodies;
        public NativeBVHTree tree;
        
        public BVHTreeWorld(int initialCapacity = 64, Allocator allocator = Allocator.Temp) : this() {
            tree = new NativeBVHTree(initialCapacity, allocator, new NativeBVHTree.Configuration { BoundsExpansion = ExpandSize});
            bodies = new NativeList<Body>(initialCapacity, allocator);
        }

        public int Add(Collider collider) {
            var index = tree.InsertLeaf(collider);
            bodies.Add(new Body {nodeId = index, collider = collider});
            return index;
        }
        
        public void UpdateTransform(int index, RigidTransform transform) {
            var body = bodies[index];
            body.transform = transform;
            bodies[index] = body;
        }

        public void Update() {
            new UpdateWorldJob {Tree = tree, Bodies = bodies}.Run();
        }

        public void Dispose() {
            tree.Dispose();
            bodies.Dispose();
        }
        
        [BurstCompile]
        public struct UpdateWorldJob : IJob {
            public NativeBVHTree Tree;
            public NativeList<Body> Bodies;

            public void Execute() {
                for (var i = 0; i < Bodies.Length; i++) {
                    var body = Bodies[i];
                    var bounds = body.collider.CalculateBounds(body.transform);
                    var union = bounds.Union(body.expandedBounds);
                    if (math.any(union.LowerBound != body.expandedBounds.LowerBound) || math.any(union.UpperBound != body.expandedBounds.UpperBound)) {
                        bounds.Expand(ExpandSize);
                        body.expandedBounds = bounds;
                        Tree.Reinsert(Bodies[i].nodeId, Bodies[i].collider, Bodies[i].transform);
                        Bodies[i] = body;
                    }
                }
            }
        }
        
        [BurstCompile]
        public struct InsertJob : IJob {
            public NativeBVHTree Tree;
            public NativeList<Body> Bodies;
            [ReadOnly] public NativeArray<Collider> Colliders;

            public void Execute() {
                for (var i = 0; i < Colliders.Length; i++) {
                    var index = Tree.InsertLeaf(Colliders[i]);
                    Bodies.Add(new Body {nodeId = index, collider = Colliders[i]});
                }
            }
        }
    }
}