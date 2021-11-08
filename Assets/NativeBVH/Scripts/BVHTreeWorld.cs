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
            return bodies.Length-1;
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
        public struct InsertCollidersJob : IJob {
            public NativeBVHTree Tree;
            public NativeList<Body> Bodies;
            [ReadOnly] public NativeArray<Collider> Colliders;

            public void Execute() {
                for (var i = 0; i < Colliders.Length; i++) {
                    var index = Tree.InsertLeaf(Colliders[i]);
                    Bodies[index] = new Body {nodeId = index, collider = Colliders[i]};
                }
            }
        }
        
        [BurstCompile]
        public struct InsertCollidersAndTransformsJob : IJob {
            public NativeBVHTree Tree;
            public NativeList<Body> Bodies;
            [ReadOnly] public NativeArray<Collider> Colliders;
            [ReadOnly] public NativeArray<RigidTransform> Transforms;

            public void Execute() {
                for (var i = 0; i < Colliders.Length; i++) {
                    var index = Tree.InsertLeaf(Colliders[i], Transforms[i]);
                    var bounds = Colliders[i].CalculateBounds(Transforms[i]);
                    bounds.Expand(ExpandSize);
                    Bodies.Add(new Body {nodeId = index, collider = Colliders[i], transform = Transforms[i], expandedBounds = bounds});
                }
            }
        }
        
        
        [BurstCompile]
        public struct CalculateJob : IJob {
            
            // https://www.forceflow.be/2013/10/07/morton-encodingdecoding-through-bit-interleaving-implementations/
            // https://github.com/johnsietsma/InfPoints/blob/master/com.infpoints/Runtime/Morton.cs
            // http://johnsietsma.com/2019/12/05/morton-order-introduction/
            // http://graphics.cs.cmu.edu/projects/aac/aac_build.pdf
            public NativeList<Body> Bodies;
            public NativeArray<AABB3D> Output;

            public void Execute() {
                int bits = 4;
                
                AABB3D bounds = Bodies.Length >= 1 ? Bodies[1].expandedBounds : new AABB3D();

                for (var j = 0; j < Bodies.Length; j++) {
                    bounds = bounds.Union(Bodies[j].expandedBounds);
                }
                
                var extents = bounds.Center - bounds.LowerBound;
                var mult = bits / extents;
                for (int i = 0; i < Bodies.Length; i++) {
                    var pos = Bodies[i].expandedBounds.Center;
                    pos -= bounds.Center; // Offset by center
                    pos.y = -pos.y; // World -> array
                    pos = (pos + extents) * .5f; // Make positive // TODO
                    pos *= mult;
                    
                    var m = Morton((uint)pos.x, (uint)pos.y, (uint)pos.z, bits);
                    bounds.Expand(m);
                }
                
                Output[0] = bounds;
            }
            
            ulong Morton(uint x, uint y, uint z, int bits){
                uint answer = 0;
                for (int i = 0; i < (8 * sizeof(uint))/3; ++i) {
                    answer |= ((x & ((uint)1 << i)) << 2*i) | ((y & ((uint)1 << i)) << (2*i + 1)) | ((z & ((uint)1 << i)) << (2*i + 2));
                }
                return answer;
            }
        }
    }
}