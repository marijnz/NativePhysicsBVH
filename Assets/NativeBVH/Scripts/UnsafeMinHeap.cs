using UnityEngine;

namespace NativeBVH {
    /// <summary>
    /// Simple min-heap.
    /// </summary>
    public unsafe static class UnsafeMinHeap {

        public struct MinHeap {
            public HeapItem* heap;
            public int count;
        }
        
        public struct HeapItem {
            public int Id;
            public float Cost;
        }
        
        public static void Push(ref MinHeap minHeap, HeapItem v) {
            // Usually this would be the place so increase size of heap if needed, but it's pre-allocated.
            minHeap.heap[minHeap.count] = v;
            SiftUp(ref minHeap, minHeap.count++);
        }

        public static HeapItem Pop(ref MinHeap minHeap) {
            var v = Top(ref minHeap);
            minHeap.heap[0] = minHeap.heap[--minHeap.count];
            if (minHeap.count > 0) SiftDown(ref minHeap, 0);
            return v;
        }

        public static HeapItem Top(ref MinHeap minHeap) {
            return minHeap.heap[0];
        }

        static void SiftUp(ref MinHeap minHeap, int n) {
            var v = minHeap.heap[n];
            for (var n2 = n / 2; n > 0 && CompareCost(v, minHeap.heap[n2]) > 0; n = n2, n2 /= 2) minHeap.heap[n] = minHeap.heap[n2];
            minHeap.heap[n] = v;
        }
        
        static void SiftDown(ref MinHeap minHeap, int n) {
            var v = minHeap.heap[n];
            for (var n2 = n * 2; n2 < minHeap.count; n = n2, n2 *= 2) {
                if (n2 + 1 < minHeap.count && CompareCost(minHeap.heap[n2 + 1], minHeap.heap[n2]) > 0) n2++;
                if (CompareCost(v, minHeap.heap[n2]) >= 0) break;
                minHeap.heap[n] = minHeap.heap[n2];
            }
            minHeap.heap[n] = v;
        }

        static int CompareCost(HeapItem a, HeapItem b) {
            if(a.Cost < b.Cost) return 1;
            if(a.Cost > b.Cost) return -1;
            return 0;
        }
    }
}
