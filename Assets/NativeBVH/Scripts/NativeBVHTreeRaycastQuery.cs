using System;
using Unity.Collections;
using Unity.Mathematics;

namespace NativeBVH {
    public unsafe partial struct NativeBVHTree : IDisposable {
        public struct Ray {
            public float3 origin;
            public float3 direction;
            public float minDistance;
            public float maxDistance;
            public uint layerMask;
        }
        
        public void RayCast(Ray ray, NativeList<int> results) {
            ray.direction = math.normalize(ray.direction);
            var invD = 1 / ray.direction;

            if (nodes[rootIndex[0]]->isLeaf) {
                RayLeaf(ref this, rootIndex[0]);
                return;
            }
            
            var stack = stackalloc int[256];
            stack[0] = rootIndex[0];
            var top = 1;
            int visits = 0;
            
            while (top > 0) {
                var index = stack[--top];
                var node = nodes[index];

                var child1 = nodes[node->child1];
                var child2 = nodes[node->child2];
                if (child1->isLeaf) {
                    RayLeaf(ref this, node->child1);
                }
                if (child2->isLeaf) {
                    RayLeaf(ref this, node->child2);
                }

                visits++;

                var result = node->grandchildrenAabbs.Raycast(ray, invD);

                ProcessResult(ref this, 0, child1->child1);
                ProcessResult(ref this, 1, child1->child2);
                ProcessResult(ref this, 2, child2->child1);
                ProcessResult(ref this, 3, child2->child2);
                
                void ProcessResult(ref NativeBVHTree tree, int childId,  int id) {
                    if (result[childId]) {
                        var child = tree. nodes[id];
                        if (child->isLeaf) {
                            RayLeaf(ref tree, id);
                        } else {
                            stack[top++] = id;
                        }
                    }
                }
            }
            
            void RayLeaf(ref NativeBVHTree tree, int id) {
                var child = tree.nodes[id];
                var transform = new RigidTransform {pos = child->box.Center}; //TODO: Use transform
                if ((ray.layerMask & child->layer) == 0 && child->collider.CastRay(ray, transform)) {
                    results.Add(id);
                }
            }
        }
    }
}