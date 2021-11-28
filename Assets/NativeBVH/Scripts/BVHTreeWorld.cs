using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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
            // https://stackoverflow.com/questions/1024754/how-to-compute-a-3d-morton-number-interleave-the-bits-of-3-ints
            public NativeList<Body> Bodies;
            public NativeArray<AABB3D> Output;

            public unsafe void Execute() {
                int dimensions = 3;
                
                // TODO: FIgure out how these relate and minimize space as much as possible
                int bits = 5;
                var apart = NextPowerOfTwo((uint) bits);
                
                var fields = 4; // NOTE: Could be dynamically lowered as optimization 
                
                // 8, 4, 2, 1
                var bitFields = stackalloc int[fields];
                for (int i = 0; i < fields; i++) {
                    bitFields[i] = int.MaxValue;
                }

                var at = 1;
                int index = fields-1;
                while (at <= apart) {
                    var bitField = new BitField32();
                    var bitIndex = 0;
                    var written = 0;
                    while (bitIndex < 32) {
                        var writtenBits = math.max(math.min(bits - written, at), 0);
                        if (writtenBits != 0) {
                            bitField.SetBits(bitIndex, true, writtenBits);
                        }
                     
                        bitIndex += at * dimensions;
                        written += at;
                    }
                  
                    at *= 2;
                    bitFields[index--] = (int) bitField.Value;
                }

                int MortonEncode(int x, int y, int z) {
                    return MortonEncodeSingle(x) | (MortonEncodeSingle(y) << 1) | (MortonEncodeSingle(z) << 2);
                }
                int MortonEncodeSingle(int x) {
                    x = (x | (x << 16)) & bitFields[0];
                    x = (x | (x <<  8)) & bitFields[1];
                    x = (x | (x <<  4)) & bitFields[2];
                    x = (x | (x <<  2)) & bitFields[3];
                    return x;
                }
                
                AABB3D bounds = Bodies.Length >= 1 ? Bodies[1].expandedBounds : new AABB3D();

                for (var j = 0; j < Bodies.Length; j++) {
                    bounds = bounds.Union(Bodies[j].expandedBounds);
                }
                
                var maxSpace = math.pow(2, bits);
                
                var extents = bounds.Center - bounds.LowerBound;
                var mult = maxSpace / extents;
                for (int i = 0; i < Bodies.Length; i++) {
                    var pos = Bodies[i].expandedBounds.Center;
                    pos -= bounds.Center; // Offset by center
                    //pos.y = -pos.y; // World -> array
                    pos = (pos + extents) * .5f; // Make positive // TODO
                    pos *= mult;
                    
                    var m = MortonEncode((int)pos.x, (int)pos.y, (int)pos.z);
                    bounds.Expand(m);

                    var p = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    p.transform.position = pos;
                    p.transform.localScale = Vector3.one * .1f;
                    var renderer = p.GetComponent<MeshRenderer>();
                    var tempMaterial = new Material(Shader.Find("Unlit/Color"));
                    tempMaterial.color = Color.Lerp(Color.blue, Color.magenta,  m / math.pow(maxSpace, 3));
                    renderer.sharedMaterial = tempMaterial;

                }
                
                Output[0] = bounds;
            }

            private static uint NextPowerOfTwo(uint v) {
                v--;
                v |= v >> 1;
                v |= v >> 2;
                v |= v >> 4;
                v |= v >> 8;
                v |= v >> 16;
                v++;
                return v;
            }
            
            ulong Morton(uint x, uint y, uint z, int bits){
                uint answer = 0;
                for (int i = 0; i < (8 * sizeof(uint))/3; ++i) {
                    answer |= ((x & ((uint)1 << i)) << 2*i) | ((y & ((uint)1 << i)) << (2*i + 1)) | ((z & ((uint)1 << i)) << (2*i + 2));
                }
                return answer;
            }
            
            
            public void RadixSort(int[] a)
            {  
                // our helper array 
                int[] t=new int[a.Length]; 

                // number of bits our group will be long 
                int r=4; // try to set this also to 2, 8 or 16 to see if it is 
                // quicker or not 

                // number of bits of a C# int 
                int b=32; 

                // counting and prefix arrays
                // (note dimensions 2^r which is the number of all possible values of a 
                // r-bit number) 
                int[] count=new int[1<<r]; 
                int[] pref=new int[1<<r]; 

                // number of groups 
                int groups=(int)Math.Ceiling((double)b/(double)r); 

                // the mask to identify groups 
                int mask = (1<<r)-1; 

                // the algorithm: 
                for (int c=0, shift=0; c<groups; c++, shift+=r) { 
                    // reset count array 
                    for (int j=0; j<count.Length; j++)
                        count[j]=0;

                    // counting elements of the c-th group 
                    for (int i=0; i<a.Length; i++)
                        count[(a[i]>>shift)&mask]++; 

                    // calculating prefixes 
                    pref[0]=0; 
                    for (int i=1; i<count.Length; i++)
                        pref[i]=pref[i-1]+count[i-1]; 

                    // from a[] to t[] elements ordered by c-th group 
                    for (int i=0; i<a.Length; i++)
                        t[pref[(a[i]>>shift)&mask]++]=a[i]; 

                    // a[]=t[] and start again until the last group 
                    t.CopyTo(a,0); 
                } 
                // a is sorted 
            }
        }
    }
}