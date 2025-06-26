using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if MAPMAGIC2
using MapMagic.Terrains;
using MapMagic.Products;
#endif

namespace GPUInstancerPro.TerrainModule
{
    public class GPUIProMapMagic2Integration : MonoBehaviour
    {
#if MAPMAGIC2
        void Start()
        {
            TerrainTile.OnTileApplied += MapMagic2TileApplied;
        }

        private void MapMagic2TileApplied(TerrainTile terrainTile, TileData tileData, StopToken stopToken)
        {
            if (terrainTile.main != null)
                terrainTile.main.terrain.gameObject.AddOrGetComponent<GPUITerrainBuiltin>();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/GPU Instancer Pro/Add Map Magic 2 Integration", validate = false, priority = 171)]
        public static void ToolbarAddMM2IntegrationManager()
        {
            GPUIProMapMagic2Integration mm2Integration = FindFirstObjectByType<GPUIProMapMagic2Integration>();
            GameObject go;
            if (mm2Integration == null)
            {
                go = new GameObject("GPUI Pro MM2 Integration");
                mm2Integration = go.AddComponent<GPUIProMapMagic2Integration>();
                Undo.RegisterCreatedObjectUndo(go, "Add GPUI Map Magic 2 Integration");

                #region Add Tree Manager
                GPUITreeManager treeManager = FindFirstObjectByType<GPUITreeManager>();
                if (treeManager == null)
                {
                    GameObject treeManagerGO = new GameObject("GPUI Tree Manager");
                    treeManagerGO.transform.SetParent(go.transform);
                    treeManager = treeManagerGO.AddComponent<GPUITreeManager>();
                    treeManager.AddTerrains(Terrain.activeTerrains);
                    treeManager.ResetPrototypesFromTerrains();
                    Undo.RegisterCreatedObjectUndo(treeManagerGO, "Add GPUI Tree Manager");
                }
                #endregion

                #region Add Detail Manager
                GPUIDetailManager detailManager = FindFirstObjectByType<GPUIDetailManager>();
                if (detailManager == null)
                {
                    GameObject detailManagerGO = new GameObject("GPUI Detail Manager");
                    detailManagerGO.transform.SetParent(go.transform);
                    detailManager = detailManagerGO.AddComponent<GPUIDetailManager>();
                    detailManager.AddTerrains(Terrain.activeTerrains);
                    detailManager.ResetPrototypesFromTerrains();
                    Undo.RegisterCreatedObjectUndo(detailManagerGO, "Add GPUI Detail Manager");
                }
                #endregion
            }
            else
                go = mm2Integration.gameObject;

            Selection.activeGameObject = go;
        }
#endif
#endif
    }
}