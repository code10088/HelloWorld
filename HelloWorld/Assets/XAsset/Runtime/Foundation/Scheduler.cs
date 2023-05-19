using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    /// <summary>
    ///     支持自动切片的调度器，可以对单帧密集的更新操作进行负载均衡处理。
    /// </summary> 
    public class Scheduler : MonoBehaviour
    {
        private static readonly Dictionary<string, RequestQueue> _Queues = new Dictionary<string, RequestQueue>();
        private static readonly List<RequestQueue> Queues = new List<RequestQueue>();
        private static readonly Queue<RequestQueue> Append = new Queue<RequestQueue>();
        private static float _realtimeSinceStartup;
        public static bool AutoSlicing { get; set; } = true;
        public static bool Working => Queues.Exists(o => o.working);

        public static bool Busy =>
            AutoSlicing && Time.realtimeSinceStartup - _realtimeSinceStartup > AutoSliceTimestep;

        public static float AutoSliceTimestep { get; set; } = 1f / 60f;
        public static byte MaxRequests { get; set; } = 10;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        private static byte updateMaxRequests { get; set; } = 10;

        private void Update()
        {
            _realtimeSinceStartup = Time.realtimeSinceStartup;
            if (Append.Count > 0)
            {
                while (Append.Count > 0)
                {
                    var item = Append.Dequeue();
                    Queues.Add(item);
                }

                Queues.Sort((x, y) => x.priority.CompareTo(y.priority));
            }

            foreach (var queue in Queues)
                if (!queue.Update())
                    break;

            if (updateMaxRequests != MaxRequests)
            {
                foreach (var queue in Queues)
                {
                    queue.maxRequests = MaxRequests;
                }

                updateMaxRequests = MaxRequests;
            }
        }

        public static void Enqueue(Request request)
        {
            var key = request.GetType().Name;
            if (!_Queues.TryGetValue(key, out var queue))
            {
                queue = new RequestQueue { key = key, maxRequests = MaxRequests, priority = request.priority };
                _Queues.Add(key, queue);
                Append.Enqueue(queue);
                // TODO: 这里可以考虑给 Request 加个优先级。
            }

            queue.Enqueue(request);
        }
    }
}