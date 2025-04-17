using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UIPanelMonitor : UnityEditor.Editor
{
    [InitializeOnLoadMethod]
    static void StartInitializeOnLoadMethod()
    {
        PrefabStage.prefabSaving += OnPrefabSaving;
    }

    static void OnPrefabSaving(GameObject go)
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
        {
            string panelDirectory = UIPanelSettings.GetPanelDirecotry();
            if (stage.assetPath.StartsWith(panelDirectory))
            {
                PanelManifest manifest = go.GetComponent<PanelManifest>();
                if (manifest == null)
                    manifest = go.AddComponent<PanelManifest>();

                UIPanelModifier.Refresh(manifest);
            }
        }
    }
}