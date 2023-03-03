using UnityEngine;

namespace UnityExtensions.Paths
{
    /// <summary>
    /// 在路径上移动
    /// </summary>
    [AddComponentMenu("Paths/Move Along Path")]
    [DisallowMultipleComponent]
    public class MoveAlongPath : ScriptableComponent
    {
        [SerializeField, GetSet("path")]
        Path _path;

        [SerializeField]
        float _distance = default;


        Path.Location _location = new Path.Location(-1, 0);


        /// <summary>
        /// 引用的路径
        /// </summary>
        public Path path
        {
            get { return _path; }
            set
            {
                if (_path != value)
                {
                    if (value && value.transform.IsChildOf(transform))
                    {
                        Debug.LogError("The Path can neither be on moving object itself, nor its children.");
                        return;
                    }

                    _path = value;
                    OnValidate();
                }
            }
        }


        /// <summary>
        /// 从路径起点开始的距离
        /// </summary>
        public float distance
        {
            get { return _distance; }
            set
            {
                _distance = value;
                OnValidate();
            }
        }


        public Path.Location location
        {
            get
            {
                return _location;
            }
        }


        /// <summary>
        /// 执行同步
        /// </summary>
        public void OnValidate()
        {
            if (_path)
            {
                _path.SetTransform(transform, _distance, ref _location);
            }
        }

    } // class MoveAlongPath

} // namespace UnityExtensions.Paths