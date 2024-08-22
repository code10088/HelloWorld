using System;

public class ArrayEx<T> where T : class
{
    T[] array;
    int count;

    public int Count => count;

    public ArrayEx(int count)
    {
        array = new T[count];
    }
    public T this[int index]
    {
        get
        {
            return array[index];
        }
        set
        {
            array[index] = value;
        }
    }
    public void Add(T t)
    {
        int index = -1;
        for (int i = 0; i < count; i++) if (array[i] == null) index = i;
        if (index < 0) index = count++;
        array[index] = t;
    }
    public void Remove(T t)
    {
        int index = FindIndex(a => a == t);
        if (index > -1) array[index] = null;
    }
    public void RemoveAt(int index)
    {
        array[index] = null;
    }
    public T Find(Predicate<T> match)
    {
        return Array.Find(array, match);
    }
    public int FindIndex(Predicate<T> match)
    {
        return Array.FindIndex(array, match);
    }
    public void Trim()
    {
        int offset = 0;
        for (int i = 0; i < count; i++)
        {
            int idx = i - offset;
            T t = array[i];
            if (t == null) offset++;
            else array[idx] = t;
        }
        for (int i = 0; i < offset; i++)
        {
            int idx = count - 1 - i;
            array[idx] = null;
        }
        count -= offset;
    }
    public void Clear()
    {
        for (int i = 0; i < count; i++) array[i] = null;
        count = 0;
    }
}
