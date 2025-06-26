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
    public class GPUICameraDataProvider : GPUIDataProvider<int, GPUICameraData>
    {
#if UNITY_EDITOR
        private Dictionary<int, GPUICameraData> _editModeCameraDataDict;
        private List<int> _editModeClearKeys;
#endif
        private Queue<int> _removalQueue;

        public override void Initialize()
        {
            base.Initialize();
            _removalQueue = new Queue<int>();
        }

        public override void ReleaseBuffers()
        {
            if (_dataDict != null)
            {
                foreach (var cd in Values)
                {
                    if (cd != null)
                        cd.ReleaseBuffers();
                }
            }
#if UNITY_EDITOR
            ClearEditModeCameraData();
#endif
            base.ReleaseBuffers();
        }

        public override void Dispose()
        {
            _removalQueue = null;

            base.Dispose();
        }

        public override bool AddOrSet(int cameraInstanceID, GPUICameraData value)
        {
            if (base.AddOrSet(cameraInstanceID, value))
            {
                GPUIRenderingSystem.Instance.UpdateCommandBuffers(value);
                return true;
            }
            return false;
        }

        public override bool Remove(int cameraInstanceID)
        {
            if (IsInitialized && _dataDict.TryGetValue(cameraInstanceID, out var cd) && cd != null)
                cd.Dispose();
            if (base.Remove(cameraInstanceID))
                return true;
            return false;
        }

        internal bool ClearEmptyCameraData()
        {
            bool result = false;
            foreach (var kvPair in _dataDict)
            {
                if (kvPair.Value == null || kvPair.Value.ActiveCamera == null)
                {
                    _removalQueue.Enqueue(kvPair.Key);
                    result = true;
                }
            }
            while (_removalQueue.TryDequeue(out int key))
                Remove(key);
            return result;
        }

        internal GPUICameraData AddCamera(Camera camera)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                if (!camera.gameObject.TryGetComponent(out GPUICamera gpuiCamera))
                    gpuiCamera = camera.gameObject.AddComponent<GPUICamera>();

                gpuiCamera.Initialize();
                AddCameraData(gpuiCamera._cameraData);
                return gpuiCamera._cameraData;
#if UNITY_EDITOR
            }
            else
                Debug.LogError(GPUIConstants.LOG_PREFIX + "AddCamera can not be used in Edit Mode. Use AddEditModeCameraData instead!");

            return null;
#endif
        }

        internal void AddCameraData(GPUICameraData cameraData)
        {
            if (cameraData.ActiveCamera != null)
            {
                if (!AddOrSet(cameraData.ActiveCamera.GetInstanceID(), cameraData))
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not add Camera Data.", cameraData.ActiveCamera.gameObject);
            }
        }

        internal void RemoveCamera(Camera camera)
        {
            if (camera != null)
            {
                Remove(camera.GetInstanceID());
            }
        }

        internal bool RegisterDefaultCamera()
        {
            Camera mainCamera = null;
            if (GPUIRuntimeSettings.Instance.cameraLoadingType != GPUICameraLoadingType.GPUICameraComponent)
            {
                mainCamera = Camera.main;
                if (mainCamera == null && GPUIRuntimeSettings.Instance.cameraLoadingType == GPUICameraLoadingType.Any)
                {
                    Camera[] cameras = Camera.allCameras;
                    if (cameras.Length > 0)
                        mainCamera = cameras[0];
                }
            }
            if (mainCamera == null)
                return false;
            AddCamera(mainCamera);
            return true;
        }

        public override bool ContainsValue(GPUICameraData value)
        {
#if UNITY_EDITOR
            if (_editModeCameraDataDict != null && _editModeCameraDataDict.ContainsKey(value.ActiveCamera.GetInstanceID()))
                return true;
#endif
            return base.ContainsValue(value);
        }

#if UNITY_EDITOR
        public int CountWithEditModeCameras => Count + (_editModeCameraDataDict == null ? 0 : _editModeCameraDataDict.Count);

        internal void AddEditModeCameraData(GPUICameraData cameraData)
        {
            if (cameraData.ActiveCamera == null)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Trying to add cameraData with null camera reference!");
#endif
                return;
            }
            if (Application.isPlaying && cameraData.ActiveCamera.cameraType != CameraType.SceneView)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Trying to add cameraData in play mode that is not SceneView camera!");
#endif
                return;
            }

            _editModeCameraDataDict ??= new();
            int key = cameraData.ActiveCamera.GetInstanceID();
            if (_editModeCameraDataDict.ContainsKey(key))
                return;
            _editModeCameraDataDict.Add(key, cameraData);
            GPUIRenderingSystem.Instance.UpdateCommandBuffers(cameraData);
        }

        internal void RemoveEditModeCameraData(GPUICameraData editModeCameraData)
        {
            if (editModeCameraData == null)
                return;
            editModeCameraData.Dispose();
            if (_editModeCameraDataDict == null || editModeCameraData.ActiveCamera == null)
                return;
            int key = editModeCameraData.ActiveCamera.GetInstanceID();
            if (_editModeCameraDataDict.ContainsKey(key))
                _editModeCameraDataDict.Remove(key);
        }

        internal void ClearEditModeCameraData()
        {
            if (_editModeCameraDataDict == null)
                return;

            foreach (var cd in _editModeCameraDataDict.Values)
                cd?.Dispose();
            _editModeCameraDataDict = null;
        }

        internal void ClearNullEditModeCameras()
        {
            if (_editModeCameraDataDict == null)
                return;
            _editModeClearKeys ??= new();

            foreach (var kv in _editModeCameraDataDict)
            {
                if (kv.Value.ActiveCamera == null)
                {
                    _editModeClearKeys.Add(kv.Key);
                    kv.Value.Dispose();
                }
            }
            foreach (int key in _editModeClearKeys)
                _editModeCameraDataDict.Remove(key);
            _editModeClearKeys.Clear();
        }

        internal bool TryGetEditModeCameraData(int cameraInstanceID, out GPUICameraData cameraData)
        {
            cameraData = null;
            if (!IsInitialized || _editModeCameraDataDict == null)
                return false;
            return _editModeCameraDataDict.TryGetValue(cameraInstanceID, out cameraData);
        }

        internal void UpdateEditModeCameraDataCommandBuffers(bool forceNew)
        {
            if (_editModeCameraDataDict == null)
                return;

            foreach (var cd in _editModeCameraDataDict.Values)
                GPUIRenderingSystem.Instance.UpdateCommandBuffers(cd, forceNew);
        }

        internal void UpdateEditModeCameraDataCommandBuffers(GPUIRenderSourceGroup rsg)
        {
            if (_editModeCameraDataDict == null)
                return;

            foreach (var cd in _editModeCameraDataDict.Values)
                rsg.UpdateCommandBuffer(cd);
        }

        public GPUICameraData GetSceneViewCameraData()
        {
            if (_editModeCameraDataDict == null)
                return null;

            foreach (var cd in _editModeCameraDataDict.Values)
            {
                if (cd.ActiveCamera != null && cd.ActiveCamera.cameraType == CameraType.SceneView)
                    return cd;
            }
            return null;
        }

        public virtual GPUICameraData GetValueAtIndexWithEditModeCameras(int index)
        {
            if (!IsInitialized)
                return null;
            if (_dataDict.Count <= index)
            {
                if (_editModeCameraDataDict == null)
                    return null;
                index -= _dataDict.Count;
                if (_editModeCameraDataDict.Count <= index)
                    return null;

                var enumerator = _editModeCameraDataDict.GetEnumerator();
                for (int i = 0; i <= index; i++)
                    enumerator.MoveNext();
                return enumerator.Current.Value;
            }
            var e = _dataDict.GetEnumerator();
            for (int i = 0; i <= index; i++)
                e.MoveNext();
            return e.Current.Value;
        }

        public void RenderEditModeCameras()
        {
            if (_editModeCameraDataDict != null)
            {
                foreach (GPUICameraData cameraData in _editModeCameraDataDict.Values)
                {
                    if (cameraData != null && cameraData.ActiveCamera != null)
                        cameraData.ActiveCamera.Render();
                }
            }
        }
#endif

    }
}