using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace NativeBVH {
	public unsafe struct UnsafeStack<T> : IDisposable where T : unmanaged {
		public UnsafeStack(Allocator allocator = Allocator.Temp, int initialCapacity = 64) : this() {
			stack = new NativeArray<T>(initialCapacity, allocator);
		}

		public int Count { get; set; }
		private NativeArray<T> stack;
		
		public void Push(T v) {
			stack[Count++] = v;
		}

		public T Pop() {
			return stack[--Count];
		}

		public void Clear() {
			UnsafeUtility.MemClear(stack.GetUnsafePtr(), sizeof(T) * Count);
		}

		public void Dispose() {
			if (stack.IsCreated) {
				stack.Dispose();
			}
		}
	}
}
