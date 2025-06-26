// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUInstancerPro
{
    public class GPUITreeProxyProvider : GPUIDataProvider<int, MeshRenderer>
    {
        private Transform _treeProxyParent;

        public override void Dispose()
        {
            if (_dataDict != null)
            {
                foreach (MeshRenderer mr in Values)
                {
                    if (mr != null)
                    {
                        Material mat = mr.sharedMaterial;
                        if (mat != null)
                            mat.DestroyGeneric();
                        if (mr.gameObject.TryGetComponent(out MeshFilter mf))
                        {
                            Mesh mesh = mf.sharedMesh;
                            if (mesh != null)
                                mesh.DestroyGeneric();
                        }
                        mr.gameObject.DestroyGeneric();
                    }
                }
            }
            _treeProxyParent.DestroyGeneric();

            base.Dispose();
        }

        public void SetTreeProxyPosition(Vector3 position)
        {
            if (_treeProxyParent != null)
                _treeProxyParent.position = position;
        }

        public void GetMaterialPropertyBlock(GPUILODGroupData lgd, MaterialPropertyBlock mpb)
        {
            MeshRenderer mr = AddOrGetTreeProxy(lgd.prototype.prefabObject);
            if (mr == null)
                return;
            mr.GetPropertyBlock(mpb);
        }

        private MeshRenderer AddOrGetTreeProxy(GameObject treePrefab)
        {
            if (!Application.isPlaying) return null;
            int key = GPUIUtility.GenerateHash(treePrefab.GetInstanceID());
            if (!_dataDict.TryGetValue(key, out MeshRenderer mr) || mr == null)
            {
                if (_treeProxyParent == null)
                    _treeProxyParent = new GameObject("GPUI Tree Proxy").transform;
                mr = AddTreeProxy(treePrefab, _treeProxyParent);
                if (mr != null)
                    AddOrSet(key, mr);
            }
            return mr;
        }

        private static MeshRenderer AddTreeProxy(GameObject treePrefab, Transform parentTransform)
        {
            Shader treeProxyShader = GPUIUtility.FindShader(GPUIConstants.SHADER_GPUI_TREE_PROXY);
            if (treeProxyShader == null)
            {
#if UNITY_EDITOR
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find GPUI Pro Tree Proxy shader! Make sure the shader is imported: " + GPUIConstants.SHADER_GPUI_TREE_PROXY);
#else
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find GPUI Pro Tree Proxy shader! Make sure the shader is included in build: " + GPUIConstants.SHADER_GPUI_TREE_PROXY);
#endif
                return null;
            }

            Mesh treeProxyMesh = new Mesh();
            treeProxyMesh.name = "TreeProxyMesh";

            Material[] treeProxyMaterials = new Material[1] { new Material(treeProxyShader) };
            LODGroup lodGroup = treePrefab.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                for (int i = 0; i < lods.Length; i++)
                {
                    for (int r = 0; r < lods[i].renderers.Length; r++)
                    {
                        var rendererGO = lods[i].renderers[r].gameObject;
                        if (rendererGO.HasComponent<Tree>() && rendererGO.HasComponent<MeshRenderer>() && rendererGO.HasComponent<MeshFilter>())
                            return InstantiateTreeProxyObject(rendererGO, parentTransform, treeProxyMaterials, treeProxyMesh);
                    }
                }
            }

            var treeComponent = treePrefab.GetComponentInChildren<Tree>();
            if (treeComponent != null)
                return InstantiateTreeProxyObject(treeComponent.gameObject, parentTransform, treeProxyMaterials, treeProxyMesh);
            var mr = treePrefab.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
                return InstantiateTreeProxyObject(mr.gameObject, parentTransform, treeProxyMaterials, treeProxyMesh);
            return null;
        }

        private static MeshRenderer InstantiateTreeProxyObject(GameObject treePrefab, Transform parentTransform, Material[] proxyMaterials, Mesh proxyMesh)
        {
            if (!treePrefab.HasComponent<MeshFilter>() || !treePrefab.HasComponent<MeshRenderer>())
                return null;
            GameObject treeProxyObject = UnityEngine.Object.Instantiate(treePrefab, parentTransform);
            treeProxyObject.hideFlags = HideFlags.DontSave;
            treeProxyObject.name = treePrefab.name + "_GPUITreeProxy";
            treeProxyObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            treeProxyObject.transform.localScale = Vector3.one;

            proxyMesh.bounds = treeProxyObject.GetComponent<MeshFilter>().sharedMesh.bounds;

            // Setup Tree Proxy object mesh renderer.
            MeshRenderer treeProxyObjectMR = treeProxyObject.GetComponent<MeshRenderer>();
            treeProxyObjectMR.shadowCastingMode = ShadowCastingMode.Off;
            treeProxyObjectMR.receiveShadows = false;
            treeProxyObjectMR.lightProbeUsage = LightProbeUsage.Off;

            for (int i = 0; i < proxyMaterials.Length; i++)
            {
                proxyMaterials[i].CopyPropertiesFromMaterial(treeProxyObjectMR.sharedMaterials[i]);
                proxyMaterials[i].enableInstancing = true;
            }

            treeProxyObjectMR.sharedMaterials = proxyMaterials;
            treeProxyObjectMR.GetComponent<MeshFilter>().sharedMesh = proxyMesh;

            StripComponents(treeProxyObject);

            return treeProxyObjectMR;
        }

        private static void StripComponents(GameObject go)
        {
            foreach (Transform child in go.transform)
                child.gameObject.DestroyGeneric();
            Component[] allComponents = go.GetComponents(typeof(Component));
            for (int i = 0; i < allComponents.Length; i++)
            {
                Component component = allComponents[i];
                if (component is Transform || component is MeshFilter || component is MeshRenderer || component is Tree)
                    continue;

                component.DestroyGeneric();
            }
        }
    }
}