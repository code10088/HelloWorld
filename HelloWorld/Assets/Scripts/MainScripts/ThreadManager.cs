using System;
using System.Collections.Generic;
using System.Threading;

namespace MainAssembly
{
    public class ThreadManager : Singletion<ThreadManager>
    {
        private int count = 0;
        private Queue<ThreadItem> wait = new Queue<ThreadItem>();
        private static ThreadItem cache = new ThreadItem();
        public int StartThread(Action<object> action, Action finish, object param = null, float time = 0)
        {
            ThreadItem temp = (ThreadItem)cache.next;
            if (temp == null) temp = new ThreadItem();
            temp.Init(action, finish, param, time);
            wait.Enqueue(temp);
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
            if (count < GameSetting.Instance.threadLimit && wait.Count > 0)
            {
                ThreadItem temp = wait.Dequeue();
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

            public void Init(Action<object> action, Action finish, object param = null, float time = 0)
            {
                base.Init(finish);
                this.action = action;
                this.param = param;
                this.time = time;
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

                next = cache.next;
                cache.next = this;
                Instance.StartThread();
            }
        }
    }
}