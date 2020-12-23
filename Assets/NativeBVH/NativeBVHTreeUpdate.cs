using System;
using Unity.Collections;
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
            tree = new NativeBVHTree(64, allocator);
            entities = new NativeList<Entity>(64, allocator);
        }

        public int Add(Collider collider) {
            var index = tree.InsertLeaf(collider);
            entities.Add(new Entity {id = index, collider = collider});
            return index;
        }

        public void Update() {
            for (var i = 0; i < entities.Length; i++) {
                var entity = entities[i];
                tree.RemoveLeaf(entity.id);
                entity.id = tree.InsertLeaf(entity.collider);
                entities[i] = entity;
            }
        }
        
        public void Dispose() {
            tree.Dispose();
            entities.Dispose();
        }
    }
}