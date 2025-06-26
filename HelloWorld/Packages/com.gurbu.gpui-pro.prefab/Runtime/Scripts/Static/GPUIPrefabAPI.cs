// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    public static class GPUIPrefabAPI
    {
        #region Prefab Module

        /// <summary>
        /// Adds the given GameObject as a prototype to the Prefab Manager
        /// </summary>
        /// <param name="prefabManager"></param>
        /// <param name="prefab"></param>
        /// <returns>Prototype index. -1 when add operation fails.</returns>
        public static int AddPrototype(GPUIPrefabManager prefabManager, GameObject prefab)
        {
            return prefabManager.AddPrototype(prefab);
        }

        /// <summary>
        /// Adds the prefab instance to an existing Prefab Manager. The corresponding prefab should be defined as a prototype on the Prefab Manager.
        /// The prefab instance will be registered on the Prefab Manager with the manager's next LateUpdate.
        /// If you wish to add the instance immediately, please use the <see cref="AddPrefabInstanceImmediate(GPUIPrefab)"/> method.
        /// </summary>
        /// <param name="gpuiPrefab"></param>
        public static void AddPrefabInstance(GPUIPrefab gpuiPrefab)
        {
            GPUIPrefabManager.AddPrefabInstance(gpuiPrefab);
        }

        /// <summary>
        /// Adds the collection of prefab instances to an existing Prefab Manager. The corresponding prefabs should be defined as a prototype on the Prefab Manager.
        /// The instances will be registered on the Prefab Manager with manager's next LateUpdate.
        /// If you wish to add the instances immediately, please use the <see cref="AddPrefabInstanceImmediate(GPUIPrefab)"/> method.
        /// </summary>
        /// <param name="gpuiPrefabs"></param>
        public static void AddPrefabInstances(IEnumerable<GPUIPrefab> gpuiPrefabs)
        {
            GPUIPrefabManager.AddPrefabInstances(gpuiPrefabs);
        }

        /// <summary>
        /// Adds the collection of instances of a specific prefab to the Prefab Manager.
        /// The instances will be registered on the Prefab Manager with manager's next LateUpdate.
        /// If you wish to add the instances immediately, please use the <see cref="AddPrefabInstanceImmediate(GPUIPrefab)"/> method.
        /// </summary>
        /// <param name="prefabManager"></param>
        /// <param name="instances">Collection of instances of a specific prefab</param>
        /// <param name="prototypeIndex">The prototype index of the prefab on the Prefab Manager</param>
        /// <returns>True, if added successfully</returns>
        public static bool AddPrefabInstances(GPUIPrefabManager prefabManager, IEnumerable<GPUIPrefab> instances, int prototypeIndex)
        {
            return prefabManager.AddPrefabInstances(instances, prototypeIndex);
        }

        /// <summary>
        /// Adds the collection of instances of a specific prefab to the Prefab Manager.
        /// The instances will be registered on the Prefab Manager with manager's next LateUpdate.
        /// If you wish to add the instances immediately, please use the <see cref="AddPrefabInstanceImmediate(GPUIPrefab)"/> method.
        /// </summary>
        /// <param name="prefabManager"></param>
        /// <param name="gameObjects">Collection of instances of a specific prefab</param>
        /// <param name="prototypeIndex">The prototype index of the prefab on the Prefab Manager</param>
        public static void AddPrefabInstances(GPUIPrefabManager prefabManager, IEnumerable<GameObject> gameObjects, int prototypeIndex)
        {
            prefabManager.AddPrefabInstances(gameObjects, prototypeIndex);
        }

        /// <summary>
        /// Adds the prefab instance to the Prefab Manager.
        /// The instances will be registered on the Prefab Manager with manager's next LateUpdate.
        /// If you wish to add the instance immediately, please use the <see cref="AddPrefabInstanceImmediate"/> method.
        /// </summary>
        /// <param name="prefabManager"></param>
        /// <param name="go"></param>
        /// <param name="prototypeIndex">The prototype index of the prefab on the Prefab Manager</param>
        /// <returns></returns>
        public static bool AddPrefabInstance(GPUIPrefabManager prefabManager, GameObject go, int prototypeIndex)
        {
            return prefabManager.AddPrefabInstance(go, prototypeIndex);
        }

        /// <summary>
        /// Adds the prefab instance to the Prefab Manager.
        /// The instances will be registered on the Prefab Manager with manager's next LateUpdate.
        /// If you wish to add the instance immediately, please use the <see cref="AddPrefabInstanceImmediate(GPUIPrefab)"/> method.
        /// </summary>
        /// <param name="prefabManager"></param>
        /// <param name="gpuiPrefab"></param>
        /// <param name="prototypeIndex">The prototype index of the prefab on the Prefab Manager</param>
        /// <returns></returns>
        public static bool AddPrefabInstance(GPUIPrefabManager prefabManager, GPUIPrefab gpuiPrefab, int prototypeIndex = -1)
        {
            return prefabManager.AddPrefabInstance(gpuiPrefab, prototypeIndex);
        }

        /// <summary>
        /// Immediately adds the prefab instance to the Prefab Manager without waiting for LateUpdate.
        /// </summary>
        /// <param name="prefabManager"></param>
        /// <param name="gpuiPrefab"></param>
        /// <param name="prototypeIndex"></param>
        /// <returns></returns>
        public static int AddPrefabInstanceImmediate(GPUIPrefabManager prefabManager, GPUIPrefab gpuiPrefab, int prototypeIndex = -1)
        {
            return prefabManager.AddPrefabInstanceImmediate(gpuiPrefab, prototypeIndex);
        }

        /// <summary>
        /// Removes the prefab instance from the Prefab Manager.
        /// </summary>
        /// <param name="gpuiPrefab"></param>
        public static void RemovePrefabInstance(GPUIPrefab gpuiPrefab)
        {
            gpuiPrefab.RemovePrefabInstance();
        }

        /// <summary>
        /// Notifies the Prefab Manager to update the transform data buffers.
        /// </summary>
        /// <param name="prefabManager"></param>
        public static void UpdateTransformData(GPUIPrefabManager prefabManager)
        {
            prefabManager.UpdateTransformData();
        }

        /// <summary>
        /// Notifies the Prefab Manager to update the transform data buffers for the specified prototype.
        /// </summary>
        /// <param name="prefabManager"></param>
        /// <param name="prototypeIndex"></param>
        public static void UpdateTransformData(GPUIPrefabManager prefabManager, int prototypeIndex)
        {
            prefabManager.UpdateTransformData(prototypeIndex);
        }

        /// <summary>
        /// Updates the Matrix4x4 data for the given prefab instance.
        /// </summary>
        /// <param name="prefabManager"></param>
        /// <param name="gpuiPrefab"></param>
        public static void UpdateTransformData(GPUIPrefabManager prefabManager, GPUIPrefab gpuiPrefab)
        {
            prefabManager.UpdateTransformData(gpuiPrefab);
        }

        #endregion Prefab Module
    }
}