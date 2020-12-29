using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Profiling;

namespace NativeBVH {
    public struct BVHTreeWorld : IDisposable {
        private const float ExpandSize = 1;

        public struct Entity {
            public Collider collider;
            public RigidTransform transform;
            public int id;
            public AABB3D expandedBounds;
        }

        public NativeList<Entity> entities;
        public NativeBVHTree tree;
        
        public BVHTreeWorld(int initialCapacity = 64, Allocator allocator = Allocator.Temp) : this() {
            tree = new NativeBVHTree(initialCapacity, allocator, new NativeBVHTree.Configuration { BoundsExpansion = ExpandSize});
            entities = new NativeList<Entity>(initialCapacity, allocator);
        }

        public int Add(Collider collider) {
            var index = tree.InsertLeaf(collider);
            entities.Add(new Entity {id = index, collider = collider});
            return index;
        }
        
        public void UpdateTransform(int index, RigidTransform transform) {
            var entity = entities[index];
            entity.transform = transform;
            entities[index] = entity;
        }

        public void Update() {
            new UpdateWorldJob {Tree = tree, Entities = entities}.Run();
        }

        public void Dispose() {
            tree.Dispose();
            entities.Dispose();
        }
        
        [BurstCompile]
        public struct UpdateWorldJob : IJob {
            public NativeBVHTree Tree;
            public NativeList<Entity> Entities;

            public void Execute() {
                for (var i = 0; i < Entities.Length; i++) {
                    var entity = Entities[i];
                    var bounds = entity.collider.CalculateBounds(entity.transform);
                    var union = bounds.Union(entity.expandedBounds);
                    if (math.any(union.LowerBound != entity.expandedBounds.LowerBound) || math.any(union.UpperBound != entity.expandedBounds.UpperBound)) {
                        bounds.Expand(ExpandSize);
                        entity.expandedBounds = bounds;
                        Tree.Reinsert(Entities[i].id, Entities[i].collider, transform:Entities[i].transform);
                        Entities[i] = entity;
                    }
                }
            }
        }
        
        [BurstCompile]
        public struct InsertJob : IJob {
            public NativeBVHTree Tree;
            public NativeList<Entity> Entities;
            [ReadOnly] public NativeArray<Collider> Leaves;

            public void Execute() {
                for (var i = 0; i < Leaves.Length; i++) {
                    var index = Tree.InsertLeaf(Leaves[i]);
                    Entities.Add(new Entity {id = index, collider = Leaves[i]});
                }
            }
        }
        
        /*
        [BurstCompile]
        public struct UpdateJob : IJob {
            public BVHTreeWorld World;
            [ReadOnly] public NativeList<int> EntityIndices;

            public void Execute() {
                for (var i = 0; i < EntityIndices.Length; i++) {
                    var entityId = EntityIndices[i];
                    
                }
            }
        }*/
    }
}