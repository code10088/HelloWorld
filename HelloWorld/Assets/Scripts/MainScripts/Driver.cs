using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

public class Driver : Singletion<Driver>
{
    #region Driver
    private ArrayEx<DriverItem> array1 = new ArrayEx<DriverItem>(100);//不切片列表
    private ArrayEx<DriverItem> array2 = new ArrayEx<DriverItem>(10000);//切片列表
    private float gcTimer = 0;
    private int idxMark = 0;
    private float timeMark = 0;

    private void Add(DriverItem item, bool ignoreFrameTime)
    {
        (ignoreFrameTime ? array1 : array2).Add(item);
    }
    public void Remove(int id, bool execMark = false)
    {
        if (id < 0) return;
        DriverItem temp = null;
        for (int i = 0; i < array1.Count; i++)
        {
            temp = array1[i];
            if (temp != null && temp.ItemID == id)
            {
                temp.endMark = true;
                temp.execMark = execMark;
                return;
            }
        }
        for (int i = 0; i < array2.Count; i++)
        {
            temp = array2[i];
            if (temp != null && temp.ItemID == id)
            {
                temp.endMark = true;
                temp.execMark = execMark;
                return;
            }
        }
    }
    public void Update()
    {
        float delta = Time.time - timeMark;
        timeMark = Time.time;
        YooAssets.Update();
        DriverItem temp;
        for (int i = 0; i < array1.Count; i++)
        {
            temp = array1[i];
            if (temp != null)
            {
                temp.Update(delta);
                if (temp.endMark)
                {
                    temp.Finish();
                    temp.Reset();
                    array1[i] = null;
                }
            }
        }
        for (int i = 0; i < array2.Count; i++)
        {
            int idx = (i + idxMark) % array2.Count;
            temp = array2[idx];
            if (temp != null)
            {
                temp.Update(delta);
                if (temp.endMark)
                {
                    temp.Finish();
                    temp.Reset();
                    array2[idx] = null;
                }
            }
            if (Time.time - timeMark > GameSetting.updateTimeSliceS)
            {
                idxMark = (idx + 1) % Math.Max(1, array2.Count);
                return;
            }
        }
        if (Time.time - gcTimer > GameSetting.gcTimeIntervalS)
        {
            array1.Trim();
            array2.Trim();

            gcTimer = Time.time;
            GC.Collect();
        }
    }
    public void GCCollect()
    {
        gcTimer = -GameSetting.gcTimeIntervalS;
    }

    private class DriverItem
    {
        private static int uniqueId = 0;
        private int itemId = -1;
        public bool endMark = false;
        public bool execMark = true;
        protected Action finish;

        public int ItemID => itemId;

        public virtual void Init(Action finish)
        {
            itemId = ++uniqueId;
            this.finish = finish;
        }
        public virtual void Update(float t)
        {

        }
        public virtual void Finish()
        {
            endMark = true;
            if (execMark) finish?.Invoke();
        }
        public virtual void Reset()
        {
            endMark = false;
            execMark = true;
            finish = null;
        }
    }
    #endregion


    #region Update
    private Queue<UpdateItem> UpdateCache = new();
    public int StartUpdate(Action<float> action, bool ignoreFrameTime = false)
    {
        UpdateItem temp = UpdateCache.Count > 0 ? UpdateCache.Dequeue() : new();
        temp.Init(action);
        Add(temp, ignoreFrameTime);
        return temp.ItemID;
    }

    private class UpdateItem : DriverItem
    {
        private Action<float> action;
        public void Init(Action<float> action)
        {
            base.Init(null);
            this.action = action;
        }
        public override void Update(float t)
        {
            if (endMark) return;
            action?.Invoke(t);
        }
        public override void Reset()
        {
            base.Reset();
            action = null;

            Instance.UpdateCache.Enqueue(this);
        }
    }
    #endregion


    #region Timer
    private Queue<TimerItem> TimerCache = new();
    /// <summary>
    /// loop>0
    ///     loop：循环时间
    ///     time：>0表示总时间，<=0表示无限循环
    ///     action：循环回调(immediately立刻执行一次)
    ///     finish：总时间结束回调
    /// loop<=0
    ///     loop：没有意义
    ///     time：总时间
    ///     action：没有意义
    ///     finish：总时间结束回调
    /// </summary>
    public int StartTimer(float time, float loop = 0f, Action<float> action = null, bool immediately = true, Action finish = null, bool ignoreFrameTime = false)
    {
        if (loop <= 0 && time <= 0) return -1;
        TimerItem temp = TimerCache.Count > 0 ? TimerCache.Dequeue() : new();
        temp.Init(time, loop, action, immediately, finish);
        Add(temp, ignoreFrameTime);
        return temp.ItemID;
    }

    private class TimerItem : DriverItem
    {
        private float time;
        private float loop;
        private Action<float> action;
        private float _time = 0;
        private float _loop = 0;

        public void Init(float time, float loop = 0f, Action<float> action = null, bool immediately = true, Action finish = null)
        {
            base.Init(finish);
            this.time = time;
            this.loop = loop;
            this.action = action;
            _time = 0;
            _loop = immediately ? 0 : loop;
        }
        public override void Update(float t)
        {
            if (endMark) return;
            _time += t;
            if (loop > 0 && _time > _loop)
            {
                _loop += loop;
                action(_time);
            }
            if (endMark) return;
            endMark = time > 0 && _time > time;
        }
        public override void Reset()
        {
            base.Reset();
            action = null;

            Instance.TimerCache.Enqueue(this);
        }
    }
    #endregion


    #region Frame
    private Queue<FrameItem> FrameCache = new();
    /// <summary>
    /// loop>0
    ///     loop：循环帧数
    ///     frame：>0表示总帧数，<=0表示无限循环
    ///     action：循环回调
    ///     finish：总帧数结束回调
    /// loop<=0
    ///     loop：没有意义
    ///     frame：总帧数
    ///     action：没有意义
    ///     finish：总帧数结束回调
    /// </summary>
    public int StartFrame(int frame, int loop = 0, Action<int> action = null, Action finish = null, bool ignoreFrameTime = false)
    {
        if (loop <= 0 && frame <= 0) return -1;
        FrameItem temp = FrameCache.Count > 0 ? FrameCache.Dequeue() : new();
        temp.Init(frame, loop, action, finish);
        Add(temp, ignoreFrameTime);
        return temp.ItemID;
    }

    private class FrameItem : DriverItem
    {
        private int frame;
        private int loop;
        private Action<int> action;
        private int _frame = 0;
        private int _loop = 0;

        public void Init(int frame, int loop = 0, Action<int> action = null, Action finish = null)
        {
            base.Init(finish);
            this.frame = frame;
            this.loop = loop;
            this.action = action;
        }
        public override void Update(float t)
        {
            if (endMark) return;
            _frame++;
            if (loop > 0 && _frame > _loop)
            {
                _loop += loop;
                action(_frame);
            }
            if (endMark) return;
            endMark = frame > 0 && _frame > frame;
        }
        public override void Reset()
        {
            base.Reset();
            frame = 0;
            loop = 0;
            action = null;
            _frame = 0;
            _loop = 0;

            Instance.FrameCache.Enqueue(this);
        }
    }
    #endregion


    #region Task
    private int taskCount = 0;
    private List<TaskItem> taskWait = new();
    /// <summary>
    /// priority=0/1，值越小优先级越高，更高优先级单独开线程
    /// 这里线程不保证立刻执行
    /// </summary>
    public int StartTask(Action<object> action, Action finish, object param = null, float time = 0, int priority = 1, bool ignoreFrameTime = false)
    {
#if UNITY_WEBGL
        action?.Invoke(param);
        finish();
        return -1;
#else
        var temp = new TaskItem();
        temp.Init(action, finish, param, time, priority, ignoreFrameTime);
        int index = taskWait.FindIndex(a => a.priority > priority);
        if (index >= 0) taskWait.Insert(index, temp);
        else taskWait.Add(temp);
        CheckTaskQueue();
        return temp.ItemID;
#endif
    }
    private void StartTask()
    {
        taskCount--;
        CheckTaskQueue();
    }
    private void CheckTaskQueue()
    {
        if (taskCount < GameSetting.taskLimit && taskWait.Count > 0)
        {
            TaskItem temp = taskWait[0];
            taskWait.RemoveAt(0);
            temp.Start();
            taskCount++;
            Add(temp, temp.ignoreFrameTime);
        }
    }

    private class TaskItem : DriverItem
    {
        private CancellationTokenSource cts;
        private Action<object> action;
        private object param;
        private float time;
        private float timer = 0;
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
        public async Task Start()
        {
            try
            {
                cts = new CancellationTokenSource();
                await Task.Run(() => action.Invoke(param), cts.Token);
            }
            catch (Exception ex)
            {
                GameDebug.LogError(ex.Message);
            }
            finally
            {
                endMark = true;
            }
        }
        public override void Update(float t)
        {
            if (endMark) return;
            timer += t;
            endMark = time > 0 && timer > time;
        }
        public override void Reset()
        {
            base.Reset();
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            action = null;
            timer = 0;

            Instance.StartTask();
        }
    }
    #endregion


    #region Coroutine
    public int StartCoroutine<T>(IEnumerator<T> enumerator, bool ignoreFrameTime = false) where T : IEnumerator
    {
        CoroutineItem<T> temp = new();
        temp.Init(enumerator);
        Add(temp, ignoreFrameTime);
        return temp.ItemID;
    }

    private class CoroutineItem<T> : DriverItem where T : IEnumerator
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
    #endregion
}
#region Coroutine
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
#endregion