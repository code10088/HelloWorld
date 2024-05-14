using System;
using System.Collections.Generic;
using System.Threading;

public class ThreadManager : Singletion<ThreadManager>
{
    private int count = 0;
    private List<ThreadItem> wait = new();
    private static Queue<ThreadItem> cache = new();

    /// <summary>
    /// priority=0/1，值越小优先级越高，更高优先级单独开线程
    /// 这里线程不保证立刻执行
    /// </summary>
    public int StartThread(Action<object> action, Action finish, object param = null, float time = 0, int priority = 1, bool ignoreFrameTime = false)
    {
#if UNITY_WEBGL
        action?.Invoke(param);
        finish();
        return -1;
#else
        ThreadItem temp = cache.Count > 0 ? cache.Dequeue() : new();
        temp.Init(action, finish, param, time, priority, ignoreFrameTime);
        int index = wait.FindIndex(a => a.priority > priority);
        if (index >= 0) wait.Insert(index, temp);
        else wait.Add(temp);
        CheckThreadQueue();
        return temp.ItemID;
#endif
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
            AsyncManager.Instance.Add(temp, temp.ignoreFrameTime);
        }
    }
    public void StopThread(int id, bool execMark = true)
    {
        if (id < 0) return;
        AsyncManager.Instance.Remove(id, execMark);
    }


    private class ThreadItem : AsyncItem
    {
        private Thread thread;
        private Action<object> action;
        private object param;
        private int timerId = -1;
        private float time;
        public int priority;
        public bool ignoreFrameTime;

        public void Init(Action<object> action, Action finish, object param, float time, int priority, bool ignoreFrameTime)
        {
            base.Init(finish);
            this.action = action;
            this.param = param;
            this.time = time;
            this.priority = priority;
            this.ignoreFrameTime = ignoreFrameTime;
        }
        public void Start()
        {
            thread = new(Run);
            thread.IsBackground = true;
            thread.Start();
            if (time > 0 && timerId < 0) timerId = TimeManager.Instance.StartTimer(time, finish: Timeout);
        }
        private void Run()
        {
            try
            {
                action?.Invoke(param);
            }
            catch(Exception ex)
            {
                GameDebug.LogError(ex.Message);
            }
            TimeManager.Instance.StopTimer(timerId);
            endMark = true;
        }
        private void Timeout()
        {
            endMark = true;
        }
        public override void Reset()
        {
            base.Reset();
            if (thread.ThreadState != ThreadState.Stopped) thread.Abort();
            thread = null;
            action = null;
            param = null;

            cache.Enqueue(this);
            Instance.StartThread();
        }
    }
}