using System.Collections.Generic;
using System;
using System.Collections;

public class UniqueStack<T> : IEnumerable<T>
{
    private Stack<T> stack;
    private HashSet<T> set;

    public UniqueStack()
    {
        stack = new Stack<T>();
        set = new HashSet<T>();
    }

    public void Push(T item)
    {
        if (!set.Contains(item))
        {
            stack.Push(item);
            set.Add(item);
        }
        else
        {
            throw new InvalidOperationException("Duplicate items are not allowed.");
        }
    }

    public T Pop()
    {
        if (stack.Count == 0)
        {
            throw new InvalidOperationException("The stack is empty.");
        }

        T item = stack.Pop();
        set.Remove(item);
        return item;
    }

    public T Peek()
    {
        if (stack.Count == 0)
        {
            throw new InvalidOperationException("The stack is empty.");
        }

        return stack.Peek();
    }

    public int Count
    {
        get { return stack.Count; }
    }

    public bool Contains(T item)
    {
        return set.Contains(item);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return stack.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}