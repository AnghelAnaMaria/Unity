using System;
using System.Collections.Generic;

public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>//clasa generica (template)
{
    private List<(TElement element, TPriority priority)> elements = new List<(TElement, TPriority)>();

    public int Count => elements.Count;

    public void Enqueue(TElement element, TPriority priority)//Adaugă un element nou în coadă
    {
        elements.Add((element, priority));//il pune la finalul listei
        HeapifyUp(elements.Count - 1);//mută în sus elementul dacă are o prioritate mai mică
    }

    public TElement Dequeue()//Scoate elementul cu cea mai mică prioritate
    {
        if (elements.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        var result = elements[0].element;//Salvează elementul din vârf ca rezultat
        elements[0] = elements[elements.Count - 1];//Pune ultimul element din listă în poziția 0
        elements.RemoveAt(elements.Count - 1);//Scoate ultimul element din listă
        HeapifyDown(0);//reordoneaza heapul
        return result;
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (elements[index].priority.CompareTo(elements[parent].priority) < 0)
            {
                Swap(index, parent);
                index = parent;
            }
            else break;
        }
    }

    private void HeapifyDown(int index)
    {
        int lastIndex = elements.Count - 1;

        while (true)
        {
            int left = 2 * index + 1;
            int right = 2 * index + 2;
            int smallest = index;

            if (left <= lastIndex && elements[left].priority.CompareTo(elements[smallest].priority) < 0)
                smallest = left;

            if (right <= lastIndex && elements[right].priority.CompareTo(elements[smallest].priority) < 0)
                smallest = right;

            if (smallest != index)
            {
                Swap(index, smallest);
                index = smallest;
            }
            else break;
        }
    }

    private void Swap(int a, int b)
    {
        var temp = elements[a];
        elements[a] = elements[b];
        elements[b] = temp;
    }
}
