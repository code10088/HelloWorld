using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ES3Internal;


/*
 * ---- How Postprocessing works for the reference manager ----
 * - When the manager is first added to the scene, all top-level dependencies are added to the manager (AddManagerToScene).
 * - When the manager is first added to the scene, all prefabs with ES3Prefab components are added to the manager (AddManagerToScene).
 * - All GameObjects and Components in the scene are added to the reference manager when we enter Playmode or the scene is saved (PlayModeStateChanged, OnWillSaveAssets -> AddGameObjectsAndComponentstoManager).
 * - When a UnityEngine.Object field of a Component is modified, the new UnityEngine.Object reference is added to the reference manager (PostProcessModifications)
 * - All prefabs with ES3Prefab Components are added to the reference manager when we enter Playmode or the scene is saved (PlayModeStateChanged, OnWillSaveAssets -> AddGameObjectsAndComponentstoManager).
 * - Local references for prefabs are processed whenever a prefab with an ES3Prefab Component is deselected (SelectionChanged -> ProcessGameObject)
 */
[InitializeOnLoad]
public class ES3Postprocessor : UnityEditor.AssetModificationProcessor
{
    public static ES3ReferenceMgr RefMgr
    {
        get { return (ES3ReferenceMgr)ES3ReferenceMgr.Current; }
    }

    public static GameObject lastSelected = null;


    // This constructor is also called once when playmode is activated and whenever recompilation happens
    // because we have the [InitializeOnLoad] attribute assigned to the class.
    static ES3Postprocessor()
    {
#if UNITY_2020_2_OR_NEWER
        ObjectChangeEvents.changesPublished += Changed;
#endif
        ObjectFactory.componentWasAdded += ComponentWasAdded;

        // Open the Easy Save 3 window the first time ES3 is installed.
        //ES3Editor.ES3Window.OpenEditorWindowOnStart();

        EditorApplication.playModeStateChanged -= PlayModeStateChanged;
        EditorApplication.playModeStateChanged += PlayModeStateChanged;

        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    #region Reference Updating

    private static void PlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            UpdateAssembliesContainingES3Types();
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (mode == OpenSceneMode.AdditiveWithoutLoading || Application.isPlaying)
            return;

        if (ES3Settings.defaultSettingsScriptableObject.autoUpdateReferences && ES3Settings.defaultSettingsScriptableObject.updateReferencesWhenSceneIsOpened)
            RefreshScene(scene);
    }

    private static void RefreshReferences(bool isEnteringPlayMode = false)
    {
        /*if (refreshed) // If we've already refreshed, do nothing.
            return;*/

        if (ES3Settings.defaultSettingsScriptableObject.autoUpdateReferences)
            for (int i = 0; i < SceneManager.sceneCount; i++)
                RefreshScene(SceneManager.GetSceneAt(i));
        //refreshed = true;
    }

    static void RefreshScene(Scene scene, bool isEnteringPlayMode = false)
    {
        if (scene != null && scene.isLoaded)
        {
            var mgr = (ES3ReferenceMgr)ES3ReferenceMgr.GetManagerFromScene(scene);
            if (mgr != null)
                mgr.RefreshDependencies(isEnteringPlayMode);
        }
    }

    static void ComponentWasAdded(Component c)
    {
        var scene = c.gameObject.scene;
        
        if (!scene.isLoaded)
            return;

        var mgr = (ES3ReferenceMgr)ES3ReferenceMgr.GetManagerFromScene(scene);

        if (mgr != null)
            mgr.AddDependencies(c);
    }

#if UNITY_2020_2_OR_NEWER
    static void Changed(ref ObjectChangeEventStream stream)
    {
        if (EditorApplication.isUpdating || Application.isPlaying || !ES3Settings.defaultSettingsScriptableObject.autoUpdateReferences || !ES3Settings.defaultSettingsScriptableObject.updateReferencesWhenSceneChanges)
            return;

            for (int i = 0; i < stream.length; i++)
        {
            var eventType = stream.GetEventType(i);
            int[] instanceIds;
            Scene scene;

            if (eventType == ObjectChangeKind.ChangeGameObjectOrComponentProperties)
            {
                ChangeGameObjectOrComponentPropertiesEventArgs evt;
                stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out evt);
                instanceIds = new int[] { evt.instanceId };
                scene = evt.scene;
            }
            else if (eventType == ObjectChangeKind.CreateGameObjectHierarchy)
            {
                CreateGameObjectHierarchyEventArgs evt;
                stream.GetCreateGameObjectHierarchyEvent(i, out evt);
                instanceIds = new int[] { evt.instanceId };
                scene = evt.scene;
            }
            /*else if (eventType == ObjectChangeKind.ChangeAssetObjectProperties)
            {
                ChangeAssetObjectPropertiesEventArgs evt;
                stream.GetChangeAssetObjectPropertiesEvent(i, out evt);
                instanceIds = new int[] { evt.instanceId };
            }*/
            else if (eventType == ObjectChangeKind.UpdatePrefabInstances)
            {
                UpdatePrefabInstancesEventArgs evt;
                stream.GetUpdatePrefabInstancesEvent(i, out evt);
                instanceIds = evt.instanceIds.ToArray();
                scene = evt.scene;
            }
            else
                continue;

            var mgr = (ES3ReferenceMgr)ES3ReferenceMgr.GetManagerFromScene(scene);

            if (mgr == null)
                return;

            foreach (var id in instanceIds)
            {
                try
                {
                    var obj = EditorUtility.InstanceIDToObject(id);

                    if (obj == null)
                        continue;

                    mgr.AddDependencies(obj);
                }
                catch { }
            }
        }
    }
#endif

    /*public static void PlayModeStateChanged(PlayModeStateChange state)
    {
        // Add all GameObjects and Components to the reference manager before we enter play mode.
        if (state == PlayModeStateChange.ExitingEditMode && ES3Settings.defaultSettingsScriptableObject.autoUpdateReferences)
            RefreshReferences(true);
    }*/

    public static string[] OnWillSaveAssets(string[] paths)
    {
        // Don't refresh references when the application is playing.
        if (!EditorApplication.isUpdating && !Application.isPlaying)
        {
            if(ES3Settings.defaultSettingsScriptableObject.autoUpdateReferences && ES3Settings.defaultSettingsScriptableObject.updateReferencesWhenSceneIsSaved)
                RefreshReferences();
            UpdateAssembliesContainingES3Types();
        }
        return paths;
    }

    #endregion


    private static void UpdateAssembliesContainingES3Types()
    {
        var assemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies();

        if (assemblies == null || assemblies.Length == 0)
            return;

        var defaults = ES3Settings.defaultSettingsScriptableObject;
        var currentAssemblyNames = defaults.settings.assemblyNames;

        var assemblyNames = new List<string>();

        foreach (var assembly in assemblies)
        {
            // Don't include Editor assemblies.
            if (assembly.flags.HasFlag(UnityEditor.Compilation.AssemblyFlags.EditorAssembly))
                continue;

            // Assemblies beginning with 'com.' are assumed to be internal.
            if (assembly.name.StartsWith("com."))
                continue;

            // If this assembly begins with 'Unity', but isn't created from an Assembly Definition File, skip it.
            if (assembly.name.StartsWith("Unity"))
            {
                bool isAssemblyDefinition = true;

                foreach (string sourceFile in assembly.sourceFiles)
                {
                    if (!sourceFile.StartsWith("Assets/"))
                    {
                        isAssemblyDefinition = false;
                        break;
                    }
                }

                if (!isAssemblyDefinition)
                    continue;
            }

            assemblyNames.Add(assembly.name);
        }

        // If there are no assembly names, 
        if (assemblyNames.Count == 0)
            return;

        // Sort it alphabetically so that the order isn't constantly changing, which can affect version control.
        assemblyNames.Sort();

        // Only update if the list has changed.
        for (int i = 0; i < assemblyNames.Count; i++)
        {
            if (currentAssemblyNames.Length != assemblyNames.Count || currentAssemblyNames[i] != assemblyNames[i])
            {
                defaults.settings.assemblyNames = assemblyNames.ToArray();
                EditorUtility.SetDirty(defaults);
                break;
            }
        }
    }

    public static GameObject AddManagerToScene()
    {
        GameObject mgr = null;
        if (RefMgr != null)
            mgr = RefMgr.gameObject;

        if (mgr == null)
            mgr = new GameObject("Easy Save 3 Manager");

        if (mgr.GetComponent<ES3ReferenceMgr>() == null)
        {
            mgr.AddComponent<ES3ReferenceMgr>();

            if (!Application.isPlaying && ES3Settings.defaultSettingsScriptableObject.autoUpdateReferences)
                RefMgr.RefreshDependencies();
        }

        if (mgr.GetComponent<ES3AutoSaveMgr>() == null)
            mgr.AddComponent<ES3AutoSaveMgr>();

        Undo.RegisterCreatedObjectUndo(mgr, "Enabled Easy Save for Scene");
        return mgr;
    }
}