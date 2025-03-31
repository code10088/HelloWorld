using System;
using System.Threading;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// Queued task
    /// </summary>
    public interface IQueuedTask<TTask> where TTask : IQueuedTask<TTask>
    {
        /// <summary>
        /// Called before adding the task, can be used to delete overdue tasks.
        /// </summary>
        /// <param name="tasks">The first task is processing currently.</param>
        /// <returns>True means this task can be added to the queue.</returns>
        bool OnEnqueue(LinkedList<TTask> tasks);

        /// <summary>
        /// Start processing this task, called before first OnUpdate.
        /// </summary>
        void OnStart();

        /// <summary>
        /// OnUpdate is called every frame after task started.
        /// </summary>
        /// <returns>True means this task is done.</returns>
        bool OnUpdate();

        /// <summary>
        /// Called after this task completed.
        /// </summary>
        void OnComplete();
    }

    public abstract class QueuedTask<TTask> : IQueuedTask<TTask> where TTask : QueuedTask<TTask>
    {
        public virtual bool OnEnqueue(LinkedList<TTask> tasks) => true;

        public virtual void OnStart() { }

        public virtual bool OnUpdate() => false;

        public virtual void OnComplete() { }
    }

    /// <summary>
    /// Task queue, supports edit-mode.
    /// </summary>
    public class TaskQueue<TTask> where TTask : IQueuedTask<TTask>
    {
        LinkedList<TTask> _tasks = new LinkedList<TTask>(8);
        bool _processing = false;

        public bool hasTask => _tasks.count > 0;

        public int taskCount => _tasks.count;

        public LinkedList<TTask> tasks => _tasks;

        public event Action<TTask> taskCompleted;

        /// <summary>
        /// Enqueue a new task.
        /// </summary>
        public bool Enqueue(TTask task)
        {
            if (task.OnEnqueue(_tasks))
            {
                if (_tasks.count == 0)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        RuntimeUtilities.unitedUpdate += Update;
                    else
#endif
                        RuntimeUtilities.waitForEndOfFrame += Update;
                }

                _tasks.AddLast(task);

                return true;
            }
            return false;
        }

        void Update()
        {
            while (true)
            {
                var task = _tasks[_tasks.first];

                if (!_processing)
                {
                    _processing = true;
                    task.OnStart();
                }

                if (task.OnUpdate())
                {
                    _processing = false;
                    task.OnComplete();
                    taskCompleted?.Invoke(task);

                    _tasks.RemoveFirst();

                    if (_tasks.count == 0)
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                            RuntimeUtilities.unitedUpdate -= Update;
                        else
#endif
                            RuntimeUtilities.waitForEndOfFrame -= Update;

                        return;
                    }
                }
                else return;
            }
        }

        /// <summary>
        /// Delete all unprocessed task.
        /// </summary>
        public void RemoveUnprocessed()
        {
            if (_tasks.count > 1)
            {
                int id = _tasks.GetNext(_tasks.first);
                while (id >= 0)
                {
                    int next = _tasks.GetNext(id);
                    _tasks.Remove(id);
                    id = next;
                }
            }
        }

        /// <summary>
        /// Wait all tasks to be completed.
        /// </summary>
        public void Wait()
        {
            while (_tasks.count > 0)
            {
                Update();
                if (_tasks.count > 0) Thread.Sleep(1);
            }
        }

    } // class TaskQueue<TTask>

} // namespace UnityExtensions