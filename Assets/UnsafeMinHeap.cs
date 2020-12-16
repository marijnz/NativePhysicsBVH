using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace NativeBVH {
	/// <summary>
	/// Simple min-heap.
	/// TODO: Dynamic resizing
	/// </summary>
	public unsafe struct UnsafeMinHeap : IDisposable {
		public struct HeapItem {
			public int Id;
			public float Cost;
		}
		
		public UnsafeMinHeap(int initialCapacity = 64, Allocator allocator = Allocator.Temp) : this() {
			heap = new NativeArray<HeapItem>(initialCapacity, allocator);
		}

		public int Count { get; set; }
		private NativeArray<HeapItem> heap;
		
		public void Push(HeapItem v) {
			// Usually this would be the place so increase size of heap if needed, but it's pre-allocated.
			heap[Count] = v;
			SiftUp(Count++);
		}

		public HeapItem Pop() {
			var v = Top();
			heap[0] = heap[--Count];
			if (Count > 0) SiftDown(0);
			return v;
		}

		public HeapItem Top() {
			if (Count > 0) return heap[0];
			return default;
		}

		public void Clear() {
			UnsafeUtility.MemClear(heap.GetUnsafePtr(), sizeof(HeapItem) * Count);
		}

		void SiftUp(int n) {
			var v = heap[n];
			for (var n2 = n / 2; n > 0 && CompareCost(v, heap[n2]) > 0; n = n2, n2 /= 2) heap[n] = heap[n2];
			heap[n] = v;
		}
		
		void SiftDown(int n) {
			var v = heap[n];
			for (var n2 = n * 2; n2 < Count; n = n2, n2 *= 2) {
				if (n2 + 1 < Count && CompareCost(heap[n2 + 1], heap[n2]) > 0) n2++;
				if (CompareCost(v, heap[n2]) >= 0) break;
				heap[n] = heap[n2];
			}
			heap[n] = v;
		}

		int CompareCost(HeapItem a, HeapItem b) {
			if(a.Cost < b.Cost) return 1;
			if(a.Cost > b.Cost) return -1;
			return 0;
		}

		public void Dispose() {
			if (heap.IsCreated) {
				heap.Dispose();
			}
		}
	}
}
