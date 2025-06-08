using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaveFunctionCollapse
{
    /// <summary>
    /// A simple binary min-heap / priority queue.  
    /// </summary>
    /// <typeparam name="T">Type of elements. Use Comparison&lt;T&gt; or IComparer&lt;T&gt; to define priority.</typeparam>
    public class Heap<T>
    {
        private readonly List<T> _data;
        private readonly Comparison<T> _comparer;

        /// <summary>
        /// Creates an empty heap.
        /// </summary>
        /// <param name="comparer">
        /// Comparison that returns &lt;0 if x has higher priority than y.
        /// </param>
        /// <param name="initialCapacity">Optional initial capacity to avoid resizing.</param>
        public Heap(Comparison<T> comparer, int initialCapacity = 0)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            _comparer = comparer;
            _data = initialCapacity > 0
                ? new List<T>(initialCapacity)
                : new List<T>();
        }

        /// <summary>
        /// Number of items in the heap.
        /// </summary>
        public int Count => _data.Count;

        /// <summary>
        /// Remove all items.
        /// </summary>
        public void Clear() => _data.Clear();

        /// <summary>
        /// Returns the smallest item without removing it.
        /// </summary>
        public T Peek()
        {
            if (_data.Count == 0)
                throw new InvalidOperationException("Heap is empty");
            return _data[0];
        }

        /// <summary>
        /// Removes and returns the smallest item.
        /// </summary>
        public T Pop()
        {
            if (_data.Count == 0)
                throw new InvalidOperationException("Heap is empty");

            T min = _data[0];
            int lastIndex = _data.Count - 1;

            // Move last to root and shrink
            _data[0] = _data[lastIndex];
            _data.RemoveAt(lastIndex);

            // Bubble down from root
            HeapifyDown(0);
            return min;
        }

        /// <summary>
        /// Tries to pop the smallest item. Returns false if empty.
        /// </summary>
        public bool TryPop(out T item)
        {
            if (_data.Count == 0)
            {
                item = default!;
                return false;
            }

            item = Pop();
            return true;
        }

        /// <summary>
        /// Pushes a new item into the heap.
        /// </summary>
        public void Push(T item)
        {
            _data.Add(item);
            HeapifyUp(_data.Count - 1);
        }

        private void HeapifyUp(int index)
        {
            // While not at root and current < parent
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (_comparer(_data[index], _data[parent]) >= 0)
                    break;

                (_data[index], _data[parent]) = (_data[parent], _data[index]);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int count = _data.Count;
            while (true)
            {
                int left = (index << 1) + 1;
                int right = left + 1;
                int smallest = index;

                if (left < count && _comparer(_data[left], _data[smallest]) < 0)
                    smallest = left;
                if (right < count && _comparer(_data[right], _data[smallest]) < 0)
                    smallest = right;

                if (smallest == index)
                    break;

                (_data[index], _data[smallest]) = (_data[smallest], _data[index]);
                index = smallest;
            }
        }
    }
}
