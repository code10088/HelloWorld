// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GPUInstancerPro
{
    public abstract class GPUIManagerWithPrototypeData<T> : GPUIManager where T : GPUIPrototypeData, new()
    {
        #region Serialized Properties
        [SerializeField]
        protected T[] _prototypeDataArray;
        #endregion Serialized Properties

        #region MonoBehaviour Methods
#if UNITY_EDITOR
        protected override void Update()
        {
            base.Update();

            if (!Application.isPlaying || !IsInitialized)
                return;

            foreach (var prototypeData in _prototypeDataArray)
            {
                prototypeData.SetParameterBufferData();
            }
        }
#endif
        #endregion MonoBehaviour Methods

        #region Initialize/Dispose
        public override void Initialize()
        {
            base.Initialize();
            OnPrototypePropertiesModified();
        }

        protected override bool RegisterRenderer(int prototypeIndex)
        {
            GPUIPrototype prototype = _prototypes[prototypeIndex];
            T prototypeData = _prototypeDataArray[prototypeIndex];
            if (prototypeData.Initialize(prototype))
            {
                if (base.RegisterRenderer(prototypeIndex))
                    return true;
                prototypeData.Dispose();
            }
            return false;
        }

        protected override void DisposeRenderer(int prototypeIndex)
        {
            base.DisposeRenderer(prototypeIndex);

            if (_prototypeDataArray == null || _prototypeDataArray.Length <= prototypeIndex) return;

            T prototypeData = _prototypeDataArray[prototypeIndex];
            if (prototypeData == null || !prototypeData.IsInitialized)
                return;
            prototypeData.Dispose();
        }

        public override void OnPrototypeEnabledStatusChanged(int prototypeIndex, bool isEnabled)
        {
            base.OnPrototypeEnabledStatusChanged(prototypeIndex, isEnabled);
            OnPrototypePropertiesModified();
        }
        #endregion Initialize/Dispose

        #region Prototype Changes
        protected override void ClearNullPrototypes()
        {
            base.ClearNullPrototypes();

            if (_prototypeDataArray == null)
                _prototypeDataArray = new T[0];
        }

        protected override void SynchronizeData()
        {
            base.SynchronizeData();

            int length = _prototypes.Length;
            if (_prototypeDataArray == null)
                _prototypeDataArray = new T[length];
            else if (_prototypeDataArray.Length != length)
                Array.Resize(ref _prototypeDataArray, length);
            for (int i = 0; i < length; i++)
            {
                if (_prototypeDataArray[i] == null)
                {
                    _prototypeDataArray[i] = new T();
                    OnNewPrototypeDataCreated(i);
                }
                _prototypeDataArray[i].FillRequiredFields();
            }
        }

        protected virtual void OnNewPrototypeDataCreated(int prototypeIndex)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(this);
#endif
        }

        public override void OnPrototypePropertiesModified()
        {
            base.OnPrototypePropertiesModified();

            if (!IsInitialized)
                return;

            for (int i = 0; i < _prototypes.Length; i++)
            {
                if (_runtimeRenderKeys[i] == 0)
                    continue;

                _prototypeDataArray[i].SetParameterBufferData();
            }
        }

        public override void RemovePrototypeAtIndex(int index)
        {
            _prototypeDataArray = _prototypeDataArray.RemoveAtAndReturn(index);
            base.RemovePrototypeAtIndex(index);
        }

        public override void RemoveAllPrototypes()
        {
            base.RemoveAllPrototypes();
            _prototypeDataArray = new T[0];
        }

        public T GetPrototypeData(int prototypeIndex)
        {
            if (prototypeIndex < 0 || _prototypeDataArray == null || prototypeIndex >= _prototypeDataArray.Length)
                return null;
            return _prototypeDataArray[prototypeIndex];
        }

        public override GPUIPrototypeData GetPrototypeDataGeneric(int prototypeIndex)
        {
            return GetPrototypeData(prototypeIndex);
        }

        public T GetPrototypeDataWithRenderKey(int renderKey)
        {
            if (_runtimeRenderKeys != null)
            {
                for (int i = 0; i < _runtimeRenderKeys.Length; i++)
                {
                    if (_runtimeRenderKeys[i] == renderKey)
                        return _prototypeDataArray[i];
                }
            }
            return null;
        }
        #endregion Prototype Changes
    }
}