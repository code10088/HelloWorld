using System;
using System.Collections.Generic;
using System.Threading;

public class ThreadManager : Singletion<ThreadManager>
{
    private int count = 0;
    private List<ThreadItem> wait = new List<ThreadItem>();
    private static ThreadItem cache = new ThreadItem();

    /// <summary>
    /// priority=0/1，值越小优先级越高，更高优先级单独开线程
    /// 这里线程不保证立刻执行
    /// </summary>
    public int StartThread(Action<object> action, Action finish, object param = null, float time = 0, int priority = 1)
    {
        ThreadItem temp = (ThreadItem)cache.next;
        if (temp == null) temp = new ThreadItem();
        else cache.next = temp.next;
        temp.Init(action, finish, param, time, priority);
        int index = wait.FindIndex(a => a.priority > priority);
        if (index >= 0) wait.Insert(index, temp);
        else wait.Add(temp);
        CheckThreadQueue();
        return temp.ItemID;
    }
    private void StartThread()
    {
        count--;
        CheckThreadQueue();
    }
    private void CheckThreadQueue()
    {
        if (count < GameSetting.threadLimit && wait.Count > 0)
        {
            ThreadItem temp = wait[0];
            wait.RemoveAt(0);
            temp.Start();
            count++;
            AsyncManager.Instance.Add(temp);
        }
    }
    public void StopThread(int id, bool execMark = true)
    {
        AsyncManager.Instance.Remove(id, execMark);
    }


    private class ThreadItem : AsyncItem
    {
        private Thread thread;
        private Action<object> action;
        private object param;
        private float time;
        public int priority;

        public void Init(Action<object> action, Action finish, object param, float time, int priority)
        {
            base.Init(finish);
            this.action = action;
            this.param = param;
            this.time = time;
            this.priority = priority;
        }
        public void Start()
        {
            thread = new Thread(Run);
            thread.IsBackground = true;
            thread.Start();
            if (time > 0) TimeManager.Instance.StartTimer(time, finish: Timeout);
        }
        private void Run()
        {
            try
            {
                action?.Invoke(param);
            }
            catch
            {

            }
            endMark = true;
        }
        private void Timeout()
        {
            endMark = true;
        }
        public override bool Update()
        {
            return endMark;
        }
        public override void Reset()
        {
            base.Reset();
            if (thread.ThreadState != ThreadState.Stopped) thread.Abort();
            thread = null;
            action = null;
            param = null;
            priority = -1;

            next = cache.next;
            cache.next = this;
            Instance.StartThread();
        }
    }
}