// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using Unity.Collections;
using UnityEngine;

namespace GPUInstancerPro
{
    /// <summary>
    /// Abstract class for storing the serialized and runtime data for each prototype
    /// </summary>
    public abstract class GPUIPrototypeData : IGPUIDisposable, IGPUIParameterBufferData
    {
        public bool IsInitialized { get; protected set; }

        #region Initialize/Dispose
        public virtual bool IsValid(bool logError, GPUIPrototype prototype) => true;

        public virtual bool Initialize(GPUIPrototype prototype)
        {
            if (IsValid(Application.isPlaying, prototype))
            {
                IsInitialized = true;
                SetParameterBufferData();
                return true;
            }
            return false;
        }

        public virtual void ReleaseBuffers() { }

        public virtual void Dispose()
        {
            ReleaseBuffers();
            IsInitialized = false;
        }

        public virtual void FillRequiredFields() { }
        #endregion Initialize/Dispose

        #region Parameter Buffer
        public virtual void SetParameterBufferData() { }

        public virtual bool TryGetParameterBufferIndex(out int index) 
        {
            return GPUIRenderingSystem.Instance.ParameterBufferIndexes.TryGetValue(this, out index);
        }
        #endregion Parameter Buffer

        public virtual NativeArray<Matrix4x4> GetTransformationMatrixArray() => default;
    }
}
