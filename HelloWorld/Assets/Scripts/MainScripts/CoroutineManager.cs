using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineManager : MonoSingletion<CoroutineManager>
{
    public int StartCoroutine<T>(IEnumerator<T> enumerator, bool ignoreFrameTime = false) where T : IEnumerator
    {
        CoroutineItem<T> temp = new();
        temp.Init(enumerator);
        AsyncManager.Instance.Add(temp, ignoreFrameTime);
        return temp.ItemID;
    }
    public void Stop(int id, bool execMark = false)
    {
        if (id < 0) return;
        AsyncManager.Instance.Remove(id, execMark);
    }


    private class CoroutineItem<T> : AsyncItem where T : IEnumerator
    {
        private IEnumerator<T> action;
        private bool nextMark = false;

        public void Init(IEnumerator<T> action)
        {
            base.Init(finish);
            this.action = action;
            nextMark = action.MoveNext();
        }
        public override void Update(float t)
        {
            if (endMark) return;
            if (nextMark) nextMark = !action.Current.MoveNext();
            if (!nextMark) nextMark = action.MoveNext();
            endMark = !nextMark;
        }
        public override void Reset()
        {
            base.Reset();
            action = null;
        }
    }
}
public interface Coroutine : IEnumerator { }
public class WaitForSeconds : Coroutine
{
    private float time = 0;
    private float _time = 0;

    public object Current => _time;

    public WaitForSeconds(float t)
    {
        time = t;
    }
    public bool MoveNext()
    {
        _time += Time.deltaTime;
        return _time >= time;
    }
    public void Reset()
    {
        time = 0;
        _time = 0;
    }
}
public class WaitForFrame : Coroutine
{
    private int count = 0;
    private int _count = 0;

    public object Current => _count;

    public WaitForFrame(int c)
    {
        count = c;
    }
    public bool MoveNext()
    {
        return ++_count >= count;
    }
    public void Reset()
    {
        count = 0;
        _count = 0;
    }
}