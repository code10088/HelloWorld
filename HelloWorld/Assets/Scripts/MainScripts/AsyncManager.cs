using System;

public class AsyncManager : Singletion<AsyncManager>
{
    private AsyncItem first = new AsyncItem();

    public void Add(AsyncItem item)
    {
        item.next = first.next;
        first.next = item;
    }
    public void Remove(int id)
    {
        AsyncItem item = first;
        while (item != null)
        {
            AsyncItem temp = item.next;
            if (temp == null) return;
            if (temp.ItemID == id) temp.mark = true;
            if (temp.mark) return;
            else item = temp;
        }
    }
	public void Update()
    {
        AsyncItem item = first;
        while (item != null)
        {
            AsyncItem temp = item.next;
            if (temp == null) return;
            temp.Update();
            if (temp.mark) item.next = temp.next;
            else item = temp;
            if (temp.mark) temp.Reset();
        }
    }
}
public class AsyncItem
{
    private static int uniqueId = 0;
    private int itemId = -1;
    public bool mark = false;
    public AsyncItem next;
    private Action finish;

    public int ItemID => itemId;

    public virtual void Init(Action finish)
    {
        itemId = ++uniqueId;
        this.finish = finish;
    }

    public virtual void Update()
    {

    }

    protected virtual void Finish()
    {
        mark = true;
        finish?.Invoke();
    }

    public virtual void Reset()
    {
        itemId = -1;
        mark = false;
        next = null;
        finish = null;
    }
}
