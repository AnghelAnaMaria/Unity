using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaveFunctionCollapse
{

    ///binary min-heap / priority queue.  
    public class Heap<T>
    {
        private readonly List<T> data;
        private readonly Comparison<T> comparer;
        public int Count => data.Count;


        public Heap(Comparison<T> comparer, int initialCapacity = 0)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            this.comparer = comparer;
            data = initialCapacity > 0 ? new List<T>(initialCapacity) : new List<T>();
        }

        public void Clear() => data.Clear();


        /// Returns the smallest item without removing it.
        public T Peek()
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Heap is empty");
            return data[0];
        }


        /// Removes and returns the smallest item.
        public T Pop()
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Heap is empty");

            T min = data[0];
            int lastIndex = data.Count - 1;

            // Move last to root and shrink
            data[0] = data[lastIndex];
            data.RemoveAt(lastIndex);

            // Bubble down from root
            HeapifyDown(0);
            return min;
        }


        /// Tries to pop the smallest item. Returns false if empty.
        public bool TryPop(out T item)
        {
            if (data.Count == 0)
            {
                item = default!;
                return false;
            }

            item = Pop();
            return true;
        }


        /// Pushes a new item into the heap.
        public void Push(T item)
        {
            data.Add(item);
            HeapifyUp(data.Count - 1);
        }

        private void HeapifyUp(int index)
        {
            // While not at root and current < parent
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (comparer(data[index], data[parent]) >= 0)
                    break;

                (data[index], data[parent]) = (data[parent], data[index]);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int count = data.Count;
            while (true)
            {
                int left = (index << 1) + 1;// copilul stâng
                int right = left + 1;// copilul drept
                int smallest = index;

                if (left < count && comparer(data[left], data[smallest]) < 0)
                    smallest = left;
                if (right < count && comparer(data[right], data[smallest]) < 0)
                    smallest = right;

                if (smallest == index)
                    break;

                (data[index], data[smallest]) = (data[smallest], data[index]);
                index = smallest;
            }
        }
    }
}
