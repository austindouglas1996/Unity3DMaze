using System.Collections.Generic;

public class PriorityQueue<T>
{
    private List<T> data;
    private IComparer<T> comparer;

    public PriorityQueue(IComparer<T> comparer)
    {
        this.data = new List<T>();
        this.comparer = comparer;
    }

    public void Enqueue(T item)
    {
        data.Add(item);
        data.Sort(comparer);
    }

    public T Dequeue()
    {
        var item = data[0];
        data.RemoveAt(0);
        return item;
    }

    public bool Contains(T item)
    {
        return data.Contains(item);
    }

    public int Count => data.Count;

    public void UpdatePriority(T item)
    {
        data.Sort(comparer);
    }
}