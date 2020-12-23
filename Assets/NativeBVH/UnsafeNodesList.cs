using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NativeBVH {
    /// <summary>
    /// Holds and array of elements and a list of empty spots for elements.
    /// When an element is removed, its index is added to the list of empty spots, ready to be re-used.
    /// This avoids the shifting of element indices upon removal (and results in an array with holes in it).
    /// </summary>
    public unsafe struct UnsafeNodesList : IDisposable{
        [NativeDisableUnsafePtrRestriction]
        public void* ptr;
        public int length;
        	
        [NativeDisableUnsafePtrRestriction]
        private UnsafeList* emptyIndices;
        
        private Allocator allocator;
        
        public static UnsafeNodesList* Create<T>(int length, Allocator allocator) where T : unmanaged {
            var handle = new AllocatorManager.AllocatorHandle { Value = (int)allocator };
            UnsafeNodesList* listData = AllocatorManager.Allocate<UnsafeNodesList>(handle);
            UnsafeUtility.MemClear(listData, UnsafeUtility.SizeOf<UnsafeList>());

            listData->allocator = allocator;
            listData->emptyIndices = UnsafeList.Create(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), length, allocator);

            if (length != 0) {
                listData->Resize<T>(length);
            }
            
            return listData;
        }

        public static void Destroy(UnsafeNodesList* listData) {
            var allocator = listData->allocator;
            listData->Dispose();
            AllocatorManager.Free(allocator, listData);
        }

        public void RemoveAt<T>(int index) where T : unmanaged {
            emptyIndices->Add(index);
            UnsafeUtility.WriteArrayElement(emptyIndices->Ptr, index, default(T));
        }

        public int Add<T>(T element) where T : unmanaged {
            if (emptyIndices->Length <= 0) {
                Resize<T>(math.max(length * 2, 2));
            }

            var index = UnsafeUtility.ReadArrayElement<int>(emptyIndices->Ptr, emptyIndices->Length-1);
            emptyIndices->RemoveAt<int>(emptyIndices->Length - 1);
            
            UnsafeUtility.WriteArrayElement(ptr, index, element);

            return index;
        }
        
        public T* Get<T>(int index) where T : unmanaged {
            return (T*) ((long) ptr + (long) index * sizeof (T));
        }

        public void Dispose() {
            if (ptr != null) {
                AllocatorManager.Free(allocator, ptr);
                allocator = Allocator.Invalid; 
                ptr = null;
                length = 0;
                UnsafeList.Destroy(emptyIndices);
            }
        }
        
        private void Resize<T>(int newLength) where T : unmanaged{
            var newPointer = AllocatorManager.Allocate(allocator, UnsafeUtility.SizeOf<T>(),  UnsafeUtility.AlignOf<T>(), newLength);

            if (length > 0) {
                var bytesToCopy = newLength *  UnsafeUtility.SizeOf<T>();
                UnsafeUtility.MemCpy(newPointer, ptr, bytesToCopy);

                if (allocator == Allocator.Invalid || ptr == null) {
                    throw new InvalidOperationException();
                }
                AllocatorManager.Free(allocator, ptr);
            }

            for (int i = newLength - 1; i >= length; i--) {
                emptyIndices->Add(i);
            }

            ptr = newPointer;
            length = newLength;
        }
    }
}