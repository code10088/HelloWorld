using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ES3Internal;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
using System;
using System.Linq;
#endif

#if UNITY_VISUAL_SCRIPTING
using Unity.VisualScripting;
[IncludeInSettings(true)]
#endif
public class ES3ReferenceMgr : ES3ReferenceMgrBase
{
#if UNITY_EDITOR

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void RefreshDependencies(bool isEnteringPlayMode = false)
    {
        // Empty the refId so it has to be refreshed.
        refId = null;

        ES3ReferenceMgrBase.isEnteringPlayMode = isEnteringPlayMode;

        // This will get the dependencies for all GameObjects and Components from the active scene.
        AddDependencies(this.gameObject.scene.GetRootGameObjects());
        AddDependenciesFromFolders();
        AddPrefabsToManager();
        RemoveNullOrInvalidValues();

        ES3ReferenceMgrBase.isEnteringPlayMode = false;
    }

    [MenuItem("Tools/Easy Save 3/Refresh References for All Scenes", false, 150)]
    static void RefreshDependenciesInAllScenes()
    {
        if (!EditorUtility.DisplayDialog("Refresh references in all scenes", "This will open each scene which is enabled in your Build Settings, refresh each reference manager, and save the scene.\n\nWe recommend making a backup of your project before doing this for the first time.", "Ok", "Cancel", DialogOptOutDecisionType.ForThisMachine, "ES3RefreshAllOptOut"))
            return;

        // Get a list of loaded scenes so we know whether we need to close them after refreshing references or not.
        var loadedScenePaths = new string[SceneManager.sceneCount];
        for (int i = 0; i < SceneManager.sceneCount; i++)
            loadedScenePaths[i] = SceneManager.GetSceneAt(i).path;

        var scenes = EditorBuildSettings.scenes;
        var sceneNameList = ""; // We use this so we can display a list of scenes at the end.

        for (int i = 0; i < scenes.Length; i++)
        {
            var buildSettingsScene = scenes[i];

            if (!buildSettingsScene.enabled)
                continue;

            if (EditorUtility.DisplayCancelableProgressBar("Refreshing references", $"Refreshing references for scene {buildSettingsScene.path}.", i / scenes.Length))
                return;

            var sceneWasOpen = loadedScenePaths.Contains(buildSettingsScene.path);
            var scene = EditorSceneManager.OpenScene(buildSettingsScene.path, OpenSceneMode.Additive);

            var mgr = ES3ReferenceMgr.GetManagerFromScene(scene);

            if (mgr != null)
            {
                try
                {
                    ((ES3ReferenceMgr)mgr).RefreshDependencies();
                }
                catch(Exception e)
                {
                    Debug.LogError($"Couldn't update references for scene {scene.name} as the following exception occurred:\n\n" + e);
                }
            }

            sceneNameList += $"{scene.name}\n";

            // If the scene wasn't originally open, save it and close it.
            if (!sceneWasOpen)
            {
                // Temporarily disable refreshing on save so that it doesn't refresh again.
                var updateReferencesOnSave = ES3Settings.defaultSettingsScriptableObject.updateReferencesWhenSceneIsSaved;
                ES3Settings.defaultSettingsScriptableObject.updateReferencesWhenSceneIsSaved = false;
                
                EditorSceneManager.SaveScene(scene);
                EditorSceneManager.CloseScene(scene, true);

                ES3Settings.defaultSettingsScriptableObject.updateReferencesWhenSceneIsSaved = updateReferencesOnSave;
            }
        }
        EditorUtility.ClearProgressBar();

        EditorUtility.DisplayDialog("References refreshed", $"Refrences updated for scenes:\n\n{sceneNameList}", "Ok", DialogOptOutDecisionType.ForThisMachine, "ES3RefreshAllCompleteOptOut");
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void Optimize()
    {
        var dependencies = EditorUtility.CollectDependencies(this.gameObject.scene.GetRootGameObjects());
        var notDependenciesOfScene = new HashSet<UnityEngine.Object>();

        foreach (var kvp in idRef)
            if (!dependencies.Contains(kvp.Value))
                notDependenciesOfScene.Add(kvp.Value);

        foreach (var obj in notDependenciesOfScene)
            Remove(obj);
    }

    /* Adds all dependencies from this scene to the manager */
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void AddDependencies()
    {
        var rootGameObjects = gameObject.scene.GetRootGameObjects();

        for (int j = 0; j < rootGameObjects.Length; j++)
        {
            var go = rootGameObjects[j];

            if (EditorUtility.DisplayCancelableProgressBar("Gathering references", "Populating reference manager with your scene dependencies so they can be saved and loaded by reference.", j / rootGameObjects.Length))
                return;

            AddDependencies(go);
        }

        EditorUtility.ClearProgressBar();
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void AddDependencies(UnityEngine.Object[] objs)
    {
        var timeStarted = EditorApplication.timeSinceStartup;
        var timeout = ES3Settings.defaultSettingsScriptableObject.collectDependenciesTimeout;

        foreach (var obj in objs)
        {
            if (obj == null || obj.name == "Easy Save 3 Manager")
                continue;

            foreach (var dependency in EditorUtility.CollectDependencies(new UnityEngine.Object[] { obj }))
            {
                if (EditorApplication.timeSinceStartup - timeStarted > timeout)
                {
                    Debug.LogWarning($"Easy Save cancelled gathering of references for object {obj.name} because it took longer than {timeout} seconds. You can increase the timeout length in Tools > Easy Save 3 > Settings > Reference Gathering Timeout, or adjust the settings so that fewer objects are referenced in your scene.");
                    return;
                }

                Add(dependency);

                if (obj is ES3Prefab prefab)
                    AddPrefabToManager(prefab);
            }
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void AddDependenciesFromFolders()
    {
        var folders = ES3Settings.defaultSettingsScriptableObject.referenceFolders;

        if (folders == null || folders.Length == 0)
            return;

        var guids = AssetDatabase.FindAssets("t:Object", folders);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            if(obj != null)
                AddDependencies(obj);
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void AddDependenciesLegacy(UnityEngine.Object[] objs)
    {
        for (int i = 0; i < objs.Length; i++)
        {
            var obj = objs[i];

            if (obj.name == "Easy Save 3 Manager")
                continue;

            var dependencies = CollectDependenciesLegacy(obj);

            foreach (var dependency in dependencies)
            {
                if (dependency != null)
                {
                    Add(dependency);

                    // Add the prefab if it's referenced by this scene.
                    if (dependency.GetType() == typeof(ES3Prefab))
                        AddPrefabToManager((ES3Prefab)dependency);
                }
            }
        }

        Undo.RecordObject(this, "Update Easy Save 3 Reference List");
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void AddDependencies(UnityEngine.Object obj)
    {
        AddDependencies(new UnityEngine.Object[] { obj });
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void GeneratePrefabReferences()
    {
        AddPrefabsToManager();
        foreach (var es3Prefab in prefabs)
            es3Prefab.GeneratePrefabReferences();
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void AddPrefabsToManager()
    {
        if (ES3Settings.defaultSettingsScriptableObject.addAllPrefabsToManager)
        {
            // Clear any null values. This isn't necessary if we're not adding all prefabs to manager as the list is cleared each time.
            if (this.prefabs.RemoveAll(item => item == null) > 0)
                Undo.RecordObject(this, "Update Easy Save 3 Reference List");

            foreach (var es3Prefab in Resources.FindObjectsOfTypeAll<ES3Prefab>())
                AddPrefabToManager(es3Prefab);
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    private void AddPrefabToManager(ES3Prefab es3Prefab)
    {
            try
            {
                if (es3Prefab != null && EditorUtility.IsPersistent(es3Prefab))
                    if(AddPrefab(es3Prefab))
                        Undo.RecordObject(this, "Update Easy Save 3 Reference List");
                es3Prefab.GeneratePrefabReferences();
            }
            catch { }
    }
#endif
}
