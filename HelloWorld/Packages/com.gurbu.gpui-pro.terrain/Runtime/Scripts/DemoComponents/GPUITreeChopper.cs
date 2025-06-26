// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.TerrainModule
{
    public class GPUITreeChopper : GPUIInputHandler
    {
        public GPUITerrain gpuiTerrain;
        public GPUITreeManager treeManager;

        private TerrainData _terrainData;
        private TerrainCollider _terrainCollider;

        private Collider _chopperCollider;
        private Bounds _removalBounds = new Bounds(Vector3.zero, Vector3.one * 0.1f);

        private TreeInstance[] _treeCache;
        private TreePrototype[] _treePrototypes;
        private TreeInstance[] _currentTreeInstances;

        private void OnEnable()
        {
            if (gpuiTerrain == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Terrain is not assigned!");
                return;
            }
            if (treeManager == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Tree Manager is not assigned!");
                return;
            }
            _terrainData = gpuiTerrain.GetComponent<Terrain>().terrainData;
            _terrainCollider = gpuiTerrain.GetComponent<TerrainCollider>();

            _treeCache = _terrainData.treeInstances;
            _currentTreeInstances = _treeCache;
            _treePrototypes = _terrainData.treePrototypes;

            _chopperCollider = GetComponent<Collider>();
            OnTriggerEnter(_terrainCollider);
        }

        private void Update()
        {
            if (GetKeyDown(KeyCode.Alpha1))
                ResetTrees();
        }

        private void OnDisable()
        {
            if (_terrainData != null)
                _terrainData.treeInstances = _treeCache; // We need to set the tree instances back to the terrainData because Unity serializes runtime changes on terrain trees.
        }

        private void ResetTrees()
        {
            GPUITerrainAPI.SetTreeInstances(gpuiTerrain, _treeCache, true);
            _currentTreeInstances = _treeCache;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == _terrainCollider)
            {
                Vector3 terrainPos = gpuiTerrain.GetPosition();
                int treeCount = _currentTreeInstances.Length;

                Bounds chopperBounds = _chopperCollider.bounds;
                for (int i = 0; i < treeCount; i++)
                {
                    TreeInstance treeInstance = _currentTreeInstances[i];
                    Vector3 treePos = Vector3.Scale(treeInstance.position, _terrainData.size) + terrainPos;
                    if (chopperBounds.Contains(treePos))
                    {
                        _removalBounds.center = treePos;
                        RemoveTreeFromTerrain(i);
                        treeCount--;
                    }
                }
            }
        }

        private void RemoveTreeFromTerrain(int treeIndex)
        {
            Debug.Log(GPUIConstants.LOG_PREFIX + "Removing tree at index: " + treeIndex + ", position: " + _removalBounds.center);
            TreeInstance treeInstance = _currentTreeInstances[treeIndex];
            _currentTreeInstances = _currentTreeInstances.RemoveAtAndReturn(treeIndex);
            GPUITerrainAPI.SetTreeInstances(gpuiTerrain, _currentTreeInstances, true);

            GenerateCutTree(_treePrototypes[treeInstance.prototypeIndex].prefab, _removalBounds.center, Quaternion.Euler(0f, treeInstance.rotation * Mathf.Rad2Deg, 0f));
        }

        private void GenerateCutTree(GameObject treePrefab, Vector3 position, Quaternion rotation)
        {
            GameObject newTree = Instantiate(treePrefab, position, rotation);
            GPUIObjectDestroyer destroyer = newTree.AddComponent<GPUIObjectDestroyer>();
            destroyer.timeToDestroy = 2f;
            if (newTree.TryGetComponent(out Collider treeCollider))
                Destroy(treeCollider);
            Rigidbody newTreeRB = newTree.AddComponent<Rigidbody>();
            newTreeRB.AddForce(transform.forward * 5f, ForceMode.Impulse);
        }
    }
}
