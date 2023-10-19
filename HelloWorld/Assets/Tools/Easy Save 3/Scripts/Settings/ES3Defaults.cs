using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ES3Defaults : ScriptableObject
{
    [SerializeField]
    public ES3SerializableSettings settings = new ES3SerializableSettings();

    public bool addMgrToSceneAutomatically = false;
    public bool autoUpdateReferences = true;
    public bool addAllPrefabsToManager = true;
    public int collectDependenciesDepth = 4;
    public int collectDependenciesTimeout = 10;
    public bool updateReferencesWhenSceneChanges = true;
    public bool updateReferencesWhenSceneIsSaved = true;
    public bool updateReferencesWhenSceneIsOpened = true;
    [Tooltip("Folders listed here will be searched for references every time the reference manager is refreshed. Path should be relative to the project folder.")]
    public string[] referenceFolders = new string[0];

    public bool logDebugInfo = false;
    public bool logWarnings = true;
    public bool logErrors = true;
}