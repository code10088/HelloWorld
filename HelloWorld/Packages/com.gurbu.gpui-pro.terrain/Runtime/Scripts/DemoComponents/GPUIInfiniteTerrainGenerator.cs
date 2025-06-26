// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace GPUInstancerPro.TerrainModule
{
    public class GPUIInfiniteTerrainGenerator : MonoBehaviour
    {
        #region Serialized Properties

        public int seed = 42;
        public Transform centerTransform;
        public float startPosOffset = 10f;
        public float updateDistance = 20f;
        public float terrainVisibilityDistance = 2048;
        [Space]
        [Header("Terrain Settings")]
        public int terrainSize = 1024;
        public float terrainHeight = 128f;
        public Material terrainMaterial;
        public TerrainLayer[] terrainLayers;
        /// <summary>
        /// Required for terrain details to be rendered in builds when GPUI Detail Manager is inactive
        /// </summary>
        public TerrainData dummyTerrain;
        [Space]
        [Header("Detail Settings")]
        public GPUIDetailManager detailManager;
        public DetailSettings[] detailSettings;
        public int detailResolution = 512;
        [Range(0.0f, 1.0f)]
        public float detailObjectDensity = 1f;
        public bool readDetailLayerFromTerrain;
        [Space]
        [Header("Tree Settings")]
        public GPUITreeManager treeManager;
        public GameObject[] treePrefabs;
        public int treeCountPerTerrain = 100;
        public Vector2 treeSizeRange = Vector2.one;

        #endregion Serialized Properties

        [Serializable]
        public struct DetailSettings
        {
            public Texture2D texture;
            public GameObject prefab;
            public Color healthyColor;
            public Color dryColor;
            public Vector2 minMaxScale;
            public Vector2 minMaxDensity;
        }

        #region Runtime Properties

        private int _baseTextureResolution = 16;
        private int _heightmapResolution;
        private HashSet<int2> _expectedTerrainPositions;
        private Dictionary<int2, Terrain> _activeTerrains;
        private List<int2> _terrainsToDestroy;
        private Vector3 _lastUpdatePosition;
        private Queue<Terrain> _terrainPool;
        private List<int[,]> _detailArrays;

        private NativeArray<float> _heightmapNative;
        private float[] _heightMap1D;
        private float[,] _heightMap;

        private TreePrototype[] _terrainTreePrototypes;
        private TreeInstance[] _treeInstances;

        private float _detailObjectDistance = 750f;
        #endregion Runtime Properties

        private void OnEnable()
        {
            Random.InitState(seed);

            centerTransform.position = new Vector3(0, terrainHeight, 0);
            _expectedTerrainPositions = new();
            _activeTerrains = new();
            _terrainsToDestroy = new();
            _terrainPool = new();

            _heightmapResolution = Mathf.FloorToInt(terrainSize / 2.0f + 1.0f);
            _heightmapNative = new NativeArray<float>(_heightmapResolution * _heightmapResolution, Allocator.Persistent);
            _heightMap1D = new float[_heightmapResolution * _heightmapResolution];
            _heightMap = new float[_heightmapResolution, _heightmapResolution];

            _detailArrays = new List<int[,]>();
            int detailPrototypeCount = detailSettings.Length;
            for (int i = 0; i < detailPrototypeCount; i++)
            {
                int[,] detailData = new int[detailResolution, detailResolution];
                for (int x = 0; x < detailResolution; x++)
                {
                    for (int y = 0; y < detailResolution; y++)
                    {
                        detailData[x, y] = Mathf.RoundToInt(Random.Range(detailSettings[i].minMaxDensity.x, detailSettings[i].minMaxDensity.y));
                    }
                }
                _detailArrays.Add(detailData);
            }

            int treePrototypeCount = treePrefabs.Length;
            _terrainTreePrototypes = new TreePrototype[treePrototypeCount];
            for (int i = 0; i < treePrototypeCount; i++)
            {
                _terrainTreePrototypes[i] = new TreePrototype()
                {
                    prefab = treePrefabs[i]
                };
            }
            _treeInstances = new TreeInstance[treeCountPerTerrain];

            Update();
            SetCenterTransformStartPosition();
        }

        private void Update()
        {
            if (Vector3.Distance(_lastUpdatePosition, centerTransform.position) > updateDistance)
            {
                _lastUpdatePosition = centerTransform.position;
                GenerateExpectedTerrainPositions();
                GenerateTerrainsFromExpectedPositions();
            }
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void SetCenterTransformStartPosition()
        {
            if (Physics.Raycast(centerTransform.position, Vector3.down, out RaycastHit hit, terrainHeight * 2, 1 << 6))
            {
                centerTransform.position = new Vector3(0, hit.point.y + startPosOffset, 0);
            }
        }

        private void Dispose()
        {
            if (_activeTerrains != null)
            {
                foreach (Terrain terrain in _activeTerrains.Values)
                {
                    if (terrain)
                    {
                        if (Application.isPlaying)
                            Destroy(terrain.gameObject);
                        else
                            DestroyImmediate(terrain.gameObject);
                    }
                }
            }
            if (_heightmapNative.IsCreated)
                _heightmapNative.Dispose();
        }

        private void GenerateExpectedTerrainPositions()
        {
            _expectedTerrainPositions.Clear();

            int x = Mathf.FloorToInt(centerTransform.position.x / terrainSize);
            int y = Mathf.FloorToInt(centerTransform.position.z / terrainSize);

            int margin = Mathf.CeilToInt(terrainVisibilityDistance / terrainSize) + 2;

            for (int i = x - margin; i <= x + margin; i++)
            {
                for (int j = y - margin; j <= y + margin; j++)
                {
                    int2 tid = new int2(i, j);
                    if (Vector3.Distance(GetTerrainIDCenter(tid, terrainSize), centerTransform.position) < terrainVisibilityDistance + terrainSize)
                        _expectedTerrainPositions.Add(tid);
                }
            }
        }

        private void GenerateTerrainsFromExpectedPositions()
        {
            _terrainsToDestroy.Clear();
            foreach (int2 tid in _activeTerrains.Keys)
            {
                if(!_expectedTerrainPositions.Contains(tid))
                {
                    _terrainsToDestroy.Add(tid);
                }
            }
            foreach (int2 tid in _terrainsToDestroy)
            {
                Terrain terrain = _activeTerrains[tid];
                _terrainPool.Enqueue(terrain);
                _activeTerrains.Remove(tid);
                terrain.gameObject.SetActive(false);
            }
            foreach (int2 tid in _expectedTerrainPositions)
            {
                if (!_activeTerrains.ContainsKey(tid))
                {
                    _activeTerrains.Add(tid, InitializeTerrainObject(tid));
                }
            }
        }

        private Terrain InitializeTerrainObject(int2 tid)
        {
            Profiler.BeginSample("GPUIInfiniteTerrainGenerator.InitializeTerrainObject");
            Vector3 terrainPosition = GetTerrainIDPosition(tid, terrainSize);

            Terrain terrain;
            if (_terrainPool.Count == 0)
            {
                GameObject terrainGameObject = new GameObject("Terrain " + tid);
                terrainGameObject.SetActive(false);
                terrainGameObject.transform.SetParent(this.transform);

                terrain = terrainGameObject.AddComponent<Terrain>();
                TerrainCollider terrainCollider = terrainGameObject.AddComponent<TerrainCollider>();
                terrain.allowAutoConnect = true;
                terrain.groupingID = 1;
                terrain.drawInstanced = true;

                terrain.materialTemplate = terrainMaterial;

                terrain.gameObject.transform.position = terrainPosition;
                SetTerrainNeighbors(tid, terrain);

                terrain.detailObjectDensity = detailObjectDensity;

                TerrainData terrainData = CreateTerrainData(terrainPosition);
                terrainCollider.terrainData = terrainData;
                terrain.terrainData = terrainData;

                terrainGameObject.layer = 6;

                SetDetailLayers(terrain);
            }
            else
            {
                terrain = _terrainPool.Dequeue();
                terrain.gameObject.transform.position = terrainPosition;
                terrain.gameObject.name = "Terrain " + tid;
                terrain.detailObjectDensity = detailObjectDensity;
                terrain.detailObjectDistance = detailManager != null && detailManager.IsInitialized ? 0 : _detailObjectDistance;
                SetTerrainNeighbors(tid, terrain);
            }
            Profiler.EndSample();

            SetHeightmapData(terrainPosition, terrain);

            SetTreeInstances(terrain);
            GPUITerrainBuiltin terrainBuiltin = terrain.AddOrGetComponent<GPUITerrainBuiltin>();
            terrainBuiltin.LoadTerrainData();
            terrain.gameObject.SetActive(true);
            if (!readDetailLayerFromTerrain)
            {
                for (int i = 0; i < detailSettings.Length; i++)
                    terrainBuiltin.SetDetailLayer(i, _detailArrays[i]); // We already have the detail array, no need to read it again from terrain
            }

            if (detailManager != null)
                GPUITerrainAPI.AddTerrain(detailManager, terrainBuiltin);
            if (treeManager != null)
                GPUITerrainAPI.AddTerrain(treeManager, terrainBuiltin);

            return terrain;
        }

        private TerrainData CreateTerrainData(Vector3 terrainPosition)
        {
            Profiler.BeginSample("GPUIInfiniteTerrainGenerator.CreateTerrainData");
            TerrainData terrainData = dummyTerrain != null ? Instantiate(dummyTerrain) : new TerrainData();

            terrainData.heightmapResolution = _heightmapResolution;
            terrainData.baseMapResolution = _baseTextureResolution; //16 is enough.
            terrainData.alphamapResolution = terrainSize;
            terrainData.terrainLayers = terrainLayers;

            //terrain size must be set after setting terrain resolution.
            terrainData.size = new Vector3(terrainSize, terrainHeight, terrainSize);

            Profiler.EndSample();
            return terrainData;
        }

        private void SetHeightmapData(Vector3 terrainPosition, Terrain terrain)
        {
            Profiler.BeginSample("GPUIInfiniteTerrainGenerator.SetHeightmapData");
            TerrainData terrainData = terrain.terrainData;
            Profiler.BeginSample("GPUIInfiniteTerrainGenerator.CreateHeightmapArray");
            JobHandle heightmapJob = new CreateHeightmapArrayJob()
            {
                heightMap = _heightmapNative,
                heightMapResolution = _heightmapResolution,
                terrainPosition = terrainPosition,
            }.Schedule(_heightmapResolution * _heightmapResolution, _heightmapResolution);
            heightmapJob.Complete();

            _heightmapNative.CopyTo(_heightMap1D);
            Buffer.BlockCopy(_heightMap1D, 0, _heightMap, 0, 4 * _heightmapResolution * _heightmapResolution);
            Profiler.EndSample();

            Profiler.BeginSample("GPUIInfiniteTerrainGenerator.SetHeights");
            terrainData.SetHeights(0, 0, _heightMap);
            Profiler.EndSample();
            Profiler.EndSample();
        }

        private void SetTerrainNeighbors(int2 tid, Terrain terrain)
        {
            _activeTerrains.TryGetValue(new int2(tid.x - 1, tid.y), out Terrain left);
            _activeTerrains.TryGetValue(new int2(tid.x, tid.y + 1), out Terrain top);
            _activeTerrains.TryGetValue(new int2(tid.x + 1, tid.y), out Terrain right);
            _activeTerrains.TryGetValue(new int2(tid.x, tid.y - 1), out Terrain bottom);

            terrain.SetNeighbors(left, top, right, bottom);
        }

        private void SetDetailLayers(Terrain terrain)
        {
            Profiler.BeginSample("GPUIInfiniteTerrainGenerator.SetDetailLayers");
            int prototypeCount = detailSettings.Length;
            if (prototypeCount == 0)
                return;

            TerrainData terrainData = terrain.terrainData;
            terrainData.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            terrainData.SetDetailResolution(detailResolution, 16);
            terrain.detailObjectDistance = detailManager != null && detailManager.IsInitialized ? 0 : _detailObjectDistance;

            DetailPrototype[] detailPrototypes = new DetailPrototype[prototypeCount];
            for (int i = 0; i < prototypeCount; i++)
            {
                if (detailSettings[i].texture != null)
                {
                    detailPrototypes[i] = new DetailPrototype()
                    {
                        noiseSeed = Random.Range(1, 10000),
                        prototypeTexture = detailSettings[i].texture,
                        usePrototypeMesh = false,
                        renderMode = DetailRenderMode.Grass,
                        healthyColor = detailSettings[i].healthyColor,
                        dryColor = detailSettings[i].dryColor,
                        useInstancing = false,
                        alignToGround = 0.5f,
                        minWidth = detailSettings[i].minMaxScale.x,
                        maxWidth = detailSettings[i].minMaxScale.y,
                        minHeight = detailSettings[i].minMaxScale.x,
                        maxHeight = detailSettings[i].minMaxScale.y,
                        useDensityScaling = true,
                    };
                }
                else
                {
                    detailPrototypes[i] = new DetailPrototype()
                    {
                        noiseSeed = Random.Range(1, 10000),
                        prototype = detailSettings[i].prefab,
                        usePrototypeMesh = true,
                        renderMode = DetailRenderMode.VertexLit,
                        healthyColor = detailSettings[i].healthyColor,
                        dryColor = detailSettings[i].dryColor,
                        useInstancing = true,
                        alignToGround = 0.5f,
                        minWidth = detailSettings[i].minMaxScale.x,
                        maxWidth = detailSettings[i].minMaxScale.y,
                        minHeight = detailSettings[i].minMaxScale.x,
                        maxHeight = detailSettings[i].minMaxScale.y,
                        useDensityScaling = true,
                    };
                }
            }
            terrainData.detailPrototypes = detailPrototypes;

            for (int i = 0; i < prototypeCount; i++)
            {
                terrainData.SetDetailLayer(0, 0, i, _detailArrays[i]);
            }

            Profiler.EndSample();
        }

        private void SetTreeInstances(Terrain terrain)
        {
            Profiler.BeginSample("GPUIInfiniteTerrainGenerator.SetTreeInstances");
            int prototypeCount = treePrefabs.Length;
            if (prototypeCount == 0 || treeCountPerTerrain <= 0)
                return;

            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPos = terrain.GetPosition();

            terrainData.treePrototypes = _terrainTreePrototypes;

            for (int i = 0; i < treeCountPerTerrain; i++)
            {
                Vector3 pos = new Vector3(Random.value, 0, Random.value);
                pos.y = terrain.SampleHeight(pos * terrainSize + terrainPos) / terrainHeight;
                float size = Random.Range(treeSizeRange.x, treeSizeRange.y);
                _treeInstances[i] = new TreeInstance()
                {
                    position = pos,
                    rotation = Random.value * 360f,
                    heightScale = size,
                    widthScale = size,
                    color = Color.white,
                    prototypeIndex = Random.Range(0, prototypeCount)
                };
            }
            terrainData.treeInstances = _treeInstances;
            Profiler.EndSample();
        }

        private Vector3 GetTerrainIDPosition(int2 tid, int terrainSize)
        {
            return new Vector3(tid.x * terrainSize, 0, tid.y * terrainSize);
        }

        private Vector3 GetTerrainIDCenter(int2 tid, int terrainSize)
        {
            return new Vector3(tid.x * terrainSize + terrainSize / 2f, 0, tid.y * terrainSize + terrainSize / 2f);
        }

        public void SetDetailObjectDensity(float density)
        {
            detailObjectDensity = density;
            if (_activeTerrains != null)
            {
                foreach (var terrain in _activeTerrains.Values)
                {
                    terrain.detailObjectDensity = density;
                }
            }
            if (detailManager != null)
                detailManager.RequireUpdate();
        }

        public void SetDetailObjectDistance(float distance)
        {
            _detailObjectDistance = distance;
            if (detailManager != null)
            {
                detailManager.SetDetailObjectDistance(distance);
                if (!detailManager.IsInitialized && _activeTerrains != null)
                {
                    foreach (var terrain in _activeTerrains.Values)
                    {
                        terrain.detailObjectDistance = distance;
                    }
                }
            }
        }

        [BurstCompile]
        struct CreateHeightmapArrayJob : IJobParallelFor
        {
            public NativeArray<float> heightMap;
            [ReadOnly]
            public int heightMapResolution;
            [ReadOnly]
            public Vector3 terrainPosition;

            public void Execute(int i)
            {
                int x = i % heightMapResolution;
                int y = i / heightMapResolution;

                float val = Mathf.PerlinNoise((x * 2f + terrainPosition.x + 1000f) / 350f, (y * 2f + terrainPosition.z + 1000f) / 350f) * 0.8f;
                val += Mathf.PerlinNoise((x * 2f + terrainPosition.x - 1000f) / 100f, (y * 2f + terrainPosition.z - 1000f) / 100f) * 0.2f;
                val = Mathf.Clamp01(val);
                heightMap[y * heightMapResolution + x] = val;
            }
        }
    }
}
