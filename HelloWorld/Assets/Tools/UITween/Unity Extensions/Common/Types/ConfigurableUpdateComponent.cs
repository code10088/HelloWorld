using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// A component you can select UpdateMode
    /// </summary>
    public abstract class ConfigurableUpdateComponent : ScriptableComponent
    {
        [SerializeField]
        UpdateMode _updateMode = UpdateMode.Update;


        bool _registered = false;


        /// <summary>
        /// Update mode
        /// </summary>
        public UpdateMode updateMode
        {
            get { return _updateMode; }
            set
            {
                if (_updateMode != value)
                {
                    if (_registered)
                    {
                        RuntimeUtilities.RemoveUpdate(_updateMode, OnUpdate);
                        RuntimeUtilities.AddUpdate(value, OnUpdate);

                        #if UNITY_EDITOR
                            _addedUpdateMode = value;
                        #endif
                    }
                    
                    _updateMode = value;
                }
            }
        }


        protected abstract void OnUpdate();


        protected virtual void OnEnable()
        {
            RuntimeUtilities.AddUpdate(_updateMode, OnUpdate);
            _registered = true;

            #if UNITY_EDITOR
                _addedUpdateMode = _updateMode;
            #endif
        }


        protected virtual void OnDisable()
        {
            RuntimeUtilities.RemoveUpdate(_updateMode, OnUpdate);
            _registered = false;
        }


#if UNITY_EDITOR

        UpdateMode _addedUpdateMode;


        protected virtual void OnValidate()
        {
            if (_registered && _addedUpdateMode != _updateMode)
            {
                RuntimeUtilities.RemoveUpdate(_addedUpdateMode, OnUpdate);
                RuntimeUtilities.AddUpdate(_updateMode, OnUpdate);
                _addedUpdateMode = _updateMode;
            }
        }

#endif

    } // class ConfigurableUpdateComponent

} // namespace UnityExtensions