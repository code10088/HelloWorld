// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GPUInstancerPro.PrefabModule
{
    public class GPUIInstanceSpawner : MonoBehaviour
    {
        public bool isRandomSeed = true;
        public int seed = 42;
        public SpawnMode spawnMode = SpawnMode.Grid;
        public int startInstanceCount;
        public List<GameObject> prefabObjects;
        public bool addInstantly = true;
        public float addSpeed = 0f;
        public float removeSpeed = 0f;
        public int maxAddCount = 1000;
        public bool3 randomRotation;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 spacing = Vector3.one;
        public Vector3 randomPosition = Vector3.zero;
        public Vector3 center;
        public float distanceFromCenter = 100;
        public float radius = 1;
        public bool addAsChildGameObject = true;
        public Vector2 minMaxScale = Vector2.one;
        public GPUIInstanceSpawnerInstantiatedPrefabEvent instantiatedPrefabEvent;

        public Text instanceCountText;
        public Text currentInstanceCountText;
        public Slider instanceCountSlider;
        public RectTransform loadingPanel; 

        private int _currentInstanceCount;
        private float _lastAddTime;
        private float _addWaitTime;

        private int _targetInstanceCount;
        public float TargetInstanceCount
        {
            get
            {
                return _targetInstanceCount;
            }
            set
            {
                _targetInstanceCount = (int)value;
                if (_targetInstanceCount < 0)
                    _targetInstanceCount = 0;
                if (instanceCountText != null)
                    instanceCountText.text = _targetInstanceCount.ToString();
            }
        }

        private List<GameObject> _instances;
        private List<GameObject> _instancesToRemove;
        private List<GPUIPrefab> _addedInstances;
        private List<GameObject>[] _addedGOs;
        private GPUIPrefabManager _prefabManager;

        public enum SpawnMode
        {
            Sphere = 0,
            Grid = 1,
            Ring = 2
        }

        private void Awake()
        {
            if (prefabObjects == null || prefabObjects.Count == 0)
            {
                enabled = false;
                return;
            }

            if (startInstanceCount < 0)
                startInstanceCount = 0;

            _instances = new();
            _instancesToRemove = new();
            _addedInstances = new();
            _addedGOs = new List<GameObject>[prefabObjects.Count];
            for (int i = 0; i < _addedGOs.Length; i++)
                _addedGOs[i] = new();

            TargetInstanceCount = startInstanceCount;
            if (instanceCountSlider != null)
                instanceCountSlider.value = _targetInstanceCount;

            instantiatedPrefabEvent ??= new();
        }

        private void OnEnable()
        {
            Random.InitState(isRandomSeed ? Random.Range(100, 100000) : seed);
            if (_prefabManager == null)
                _prefabManager = FindAnyObjectByType<GPUIPrefabManager>();
            _lastAddTime = float.MinValue;
            if (!addInstantly && addSpeed > 0)
                _addWaitTime = 1f / addSpeed;
            else
                _addWaitTime = 0f;
        }

        private void Update()
        {
            if (_instances.Count > _targetInstanceCount)
                RemoveInstance();
            else if (_instances.Count < _targetInstanceCount)
                AddInstance();
            else if (loadingPanel != null && loadingPanel.gameObject.activeSelf)
                loadingPanel.gameObject.SetActive(false);
            ApplyDelayedRemoval();
            UpdateCurrentInstanceCount();
        }

        [ContextMenu("Spawn Instances")]
        private void SpawnInstances()
        {
            if (!Application.isPlaying)
                Awake();
            if (_instances == null)
                return;
            while (_instances.Count > _targetInstanceCount || _instances.Count < _targetInstanceCount)
                Update();
        }

        private void AddInstance()
        {
            float currentTime = Time.time;
            if (_addWaitTime > 0 && _addWaitTime > currentTime - _lastAddTime)
                return;
            int addedCount = 0;
            Transform parentTransform = transform;
            Vector3 spawnerPos = parentTransform.position;
            Quaternion spawnerRot = parentTransform.rotation;
            Vector3 spawnerScale = parentTransform.lossyScale;
            bool hasPrefabManager = _prefabManager != null;
            do
            {
                int index = _instances.Count;
                Vector3 pos = Vector3.zero;
                Vector3 eulerAngles = rotation.eulerAngles;
                if (randomRotation.x) eulerAngles.x = Random.Range(0f, 360f);
                if (randomRotation.y) eulerAngles.y = Random.Range(0f, 360f);
                if (randomRotation.z) eulerAngles.z = Random.Range(0f, 360f);
                Quaternion rot = Quaternion.Euler(eulerAngles);
                switch (spawnMode)
                {
                    case SpawnMode.Sphere:
                        pos = Random.insideUnitSphere * radius + center;
                        break;
                    case SpawnMode.Grid:
                        int sqrt = Mathf.FloorToInt(Mathf.Sqrt(index));
                        int checkVal = sqrt * sqrt + sqrt;
                        pos.x = index >= checkVal ? index - checkVal : sqrt;
                        pos.z = index >= checkVal ? sqrt : index - sqrt * sqrt;
                        pos.Scale(spacing);
                        pos = spawnerRot * pos;
                        pos += spawnerPos + new Vector3(Random.Range(-randomPosition.x, randomPosition.x), Random.Range(-randomPosition.y, randomPosition.y), Random.Range(-randomPosition.z, randomPosition.z));
                        break;
                    case SpawnMode.Ring:
                        pos = Random.insideUnitSphere * radius + center;
                        float angle = Random.value * 360f;
                        pos.x += Mathf.Cos(angle) * distanceFromCenter;
                        pos.z += Mathf.Sin(angle) * distanceFromCenter;
                        pos.Scale(spawnerScale);
                        pos = spawnerRot * pos;
                        pos += spawnerPos;
                        break;
                }
                int prefabIndex = Random.Range(0, prefabObjects.Count);
                GameObject prefabObject = prefabObjects[prefabIndex];
                GameObject objectToAdd;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    objectToAdd = (GameObject)PrefabUtility.InstantiatePrefab(prefabObject, addAsChildGameObject ? parentTransform : null);
                    objectToAdd.transform.position = pos;
                    objectToAdd.transform.rotation = rot;
                    Undo.RegisterCreatedObjectUndo(objectToAdd, "Spawn instance");
                }
                else
                {
#endif
                    objectToAdd = Instantiate(prefabObject, pos, rot, addAsChildGameObject ? parentTransform : null);
                    instantiatedPrefabEvent.Invoke(objectToAdd);
#if UNITY_EDITOR
                }
#endif
                objectToAdd.name += " [" + index + "]";
                objectToAdd.transform.localScale = Vector3.one * (Random.value * (minMaxScale.y - minMaxScale.x) + minMaxScale.x);
                _instances.Add(objectToAdd);
                if (objectToAdd.TryGetComponent(out GPUIPrefab gpuiPrefab))
                    _addedInstances.Add(gpuiPrefab);
                else if (hasPrefabManager)
                    _addedGOs[prefabIndex].Add(objectToAdd);
                addedCount++;
                _lastAddTime = currentTime;
            }
            while (addInstantly && _instances.Count < _targetInstanceCount && addedCount < maxAddCount);

            if (_addedInstances.Count > 0)
                GPUIPrefabAPI.AddPrefabInstances(_addedInstances);
            _addedInstances.Clear();

            if (hasPrefabManager)
            {
                for (int i = 0; i < _addedGOs.Length; i++)
                {
                    if (_addedGOs[i].Count > 0)
                    {
                        int prototypeIndex = _prefabManager.GetPrototypeIndex(prefabObjects[i]);
                        if (prototypeIndex >= 0)
                            _prefabManager.AddPrefabInstances(_addedGOs[i], prototypeIndex);
                    }
                    _addedGOs[i].Clear();
                }
            }
        }

        private void RemoveInstance()
        {
            if (removeSpeed <= 0)
            {
                for (int i = _instances.Count - 1; i >= _targetInstanceCount; i--)
                {
                    GameObject toRemove = _instances[i];
                    _instances.RemoveAt(i);
                    if (toRemove == null)
                        continue;
                    if (toRemove.TryGetComponent(out GPUIPrefab gpuiPrefab)&& gpuiPrefab.IsInstanced)
                        GPUIPrefabAPI.RemovePrefabInstance(gpuiPrefab);
                    Destroy(toRemove);
                }
            }
            else
            {
                _instancesToRemove.Add(_instances[_instances.Count - 1]);
                _instances.RemoveAt(_instances.Count - 1);
            }
        }

        private void ApplyDelayedRemoval()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _instancesToRemove.Count; i++)
            {
                GameObject go = _instancesToRemove[i];
                if (go == null)
                {
                    _instancesToRemove.RemoveAt(i);
                    i--;
                    continue;
                }
                go.transform.position = Vector3.MoveTowards(go.transform.position, Vector3.zero, removeSpeed * deltaTime * 100f);
                if (Vector3.Distance(go.transform.position, Vector3.zero) < 0.5f)
                {
                    if (go.TryGetComponent(out GPUIPrefab gpuiPrefab) && gpuiPrefab.IsInstanced)
                        gpuiPrefab.RemovePrefabInstance();
                    Destroy(go);
                    _instancesToRemove.RemoveAt(i);
                    i--;
                }
            }
        }

        private void UpdateCurrentInstanceCount()
        {
            if (_currentInstanceCount != _instances.Count + _instancesToRemove.Count)
            {
                _currentInstanceCount = _instances.Count + _instancesToRemove.Count;
                if (currentInstanceCountText != null)
                    currentInstanceCountText.text = _currentInstanceCount.ToString();
            }
        }

        public void AddInstances(int amount)
        {
            TargetInstanceCount += amount;
        }

        public void RemoveInstances(int amount)
        {
            TargetInstanceCount -= amount;
        }

        [Serializable]
        public class GPUIInstanceSpawnerInstantiatedPrefabEvent : UnityEvent<GameObject> { }
    }
}