using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Profiling;

namespace NativeBVH {
    public struct BVHTreeDynamicWorld : IDisposable {
        
        public struct Entity {
            public Collider collider;
            public int id;
        }

        public NativeList<Entity> entities;

        public NativeBVHTree tree;
		
        public BVHTreeDynamicWorld(int initialCapacity = 64, Allocator allocator = Allocator.Temp) : this() {
            tree = new NativeBVHTree(initialCapacity, allocator);
            entities = new NativeList<Entity>(initialCapacity, allocator);
        }

        public int Add(Collider collider) {
            var index = tree.InsertLeaf(collider);
            entities.Add(new Entity {id = index, collider = collider});
            return index;
        }

        public void Dispose() {
            tree.Dispose();
            entities.Dispose();
        }
        
        [BurstCompile]
        public struct UpdateJob : IJob {
            public BVHTreeDynamicWorld World;

            public void Execute() {
                for (var i = 0; i < World.entities.Length; i++) {
                    var entity = World.entities[i];
                    World.tree.RemoveLeaf(entity.id);
                    entity.id = World.tree.InsertLeaf(entity.collider);
                    World.entities[i] = entity;
                }
            }
        }
    }
}