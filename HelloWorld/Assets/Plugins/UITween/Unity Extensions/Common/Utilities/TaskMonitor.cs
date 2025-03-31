using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityExtensions
{
    /// <summary>
    /// TaskMonitor, Support edit-mode
    /// </summary>
    public struct TaskMonitor
    {
        struct Item
        {
            public IAsyncResult task;
            public Action<IAsyncResult> update;
            public Action<IAsyncResult> completed;
        }


        static Dictionary<IAsyncResult, int> _ids = new Dictionary<IAsyncResult, int>();
        static LinkedList<Item> _items = new LinkedList<Item>(4);
        
        static int _current = -1;   // for 'while' in Update


        /// <summary>
        /// Monitor a task, trigger callback after it completed
        /// </summary>
        public static bool Add(IAsyncResult task, Action<IAsyncResult> completed = null, Action<IAsyncResult> update = null)
        {
            if (task == null || _ids.ContainsKey(task)) return false;

            if (_items.count == 0) RuntimeUtilities.unitedUpdate += Update;

            int id = _items.AddFirst(new Item { task = task, completed = completed, update = update });
            _ids.Add(task, id);

            return true;
        }


        /// <summary>
        /// Monitor a task, trigger callback after it completed
        /// </summary>
        public static Task Add(Action asyncAction, Action<Task> completed = null, Action<Task> update = null)
        {
            var task = Task.Run(asyncAction);
            Add(task, t=>completed?.Invoke(t as Task), t=>update?.Invoke(t as Task));
            return task;
        }


        /// <summary>
        /// Monitor a task, trigger callback after it completed
        /// </summary>
        public static Task<TResult> Add<TResult>(Func<TResult> asyncFunc, Action<Task<TResult>> completed = null, Action<Task<TResult>> update = null)
        {
            var task = Task.Run(asyncFunc);
            Add(task, t => completed?.Invoke(t as Task<TResult>), t => update?.Invoke(t as Task<TResult>));
            return task;
        }


        /// <summary>
        /// Remove a monitored task
        /// </summary>
        public static bool Remove(IAsyncResult task)
        {
            if (!_ids.TryGetValue(task, out int id)) return false;

            if (_current == id) _current = _items.GetNext(id);

            _ids.Remove(task);
            _items.Remove(id);

            if (_items.count == 0) RuntimeUtilities.unitedUpdate -= Update;

            return true;
        }


        static void Update()
        {
            Item item;
            while (true)
            {
                // foreach by this way support calling Add & Remove in update & completed callbacks

                if (_current == -1) _current = _items.last;
                else _current = _items.GetPrevious(_current);

                if (_current >= 0)
                {
                    item = _items.GetNode(_current).value;

                    item.update?.Invoke(item.task);
                    if (item.task.IsCompleted)
                    {
                        if (Remove(item.task))
                            item.completed?.Invoke(item.task);
                    }
                }
                else return;
            }
        }

    } // struct TaskMonitor

} // namespace UnityExtensions
