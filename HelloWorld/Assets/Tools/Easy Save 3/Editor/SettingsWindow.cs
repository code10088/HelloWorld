using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ES3Internal;

namespace ES3Editor
{
	public class SettingsWindow : SubWindow
	{
		public ES3Defaults editorSettings = null;
		public ES3SerializableSettings settings = null;
		public SerializedObject so = null;
		public SerializedProperty referenceFoldersProperty = null;

        private Vector2 scrollPos = Vector2.zero;

		public SettingsWindow(EditorWindow window) : base("Settings", window){}

        public void OnEnable()
        {

        }

		public override void OnGUI()
		{
			if(settings == null || editorSettings == null)
				Init();

            var style = EditorStyle.Get;

            var labelWidth = EditorGUIUtility.labelWidth;


            EditorGUI.BeginChangeCheck();

            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos, style.area))
            {
                scrollPos = scrollView.scrollPosition;

                EditorGUIUtility.labelWidth = 160;

                GUILayout.Label("Runtime Settings", style.heading);

                using (new EditorGUILayout.VerticalScope(style.area))
                {
                    ES3SettingsEditor.Draw(settings);
                }

                GUILayout.Label("Debug Settings", style.heading);

                using (new EditorGUILayout.VerticalScope(style.area))
                {
                    EditorGUIUtility.labelWidth = 100;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("Log Info");
                        editorSettings.logDebugInfo = EditorGUILayout.Toggle(editorSettings.logDebugInfo);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("Log Warnings");
                        editorSettings.logWarnings = EditorGUILayout.Toggle(editorSettings.logWarnings);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("Log Errors");
                        editorSettings.logErrors = EditorGUILayout.Toggle(editorSettings.logErrors);
                    }

                    EditorGUILayout.Space();
                }

                GUILayout.Label("Editor Settings", style.heading);

                using (new EditorGUILayout.VerticalScope(style.area))
                {
                    EditorGUIUtility.labelWidth = 170;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("Auto Update References");
                        editorSettings.autoUpdateReferences = EditorGUILayout.Toggle(editorSettings.autoUpdateReferences);
                    }

                    if (editorSettings.autoUpdateReferences)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var content = new GUIContent("-- When changes are made", "Should Easy Save update the reference manager when objects in your scene changes?");
                            editorSettings.updateReferencesWhenSceneChanges = EditorGUILayout.Toggle(content, editorSettings.updateReferencesWhenSceneChanges);
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var content = new GUIContent("-- When scene is saved", "Should Easy Save update the reference manager when objects in your scene is saved?");
                            editorSettings.updateReferencesWhenSceneIsSaved = EditorGUILayout.Toggle(content, editorSettings.updateReferencesWhenSceneIsSaved);
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            var content = new GUIContent("-- When scene is opened", "Should Easy Save update the reference manager you open a scene in the Editor?");
                            editorSettings.updateReferencesWhenSceneIsOpened = EditorGUILayout.Toggle(content, editorSettings.updateReferencesWhenSceneIsOpened);
                        }
                        EditorGUILayout.Space();
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        so.Update();
                        EditorGUILayout.PropertyField(referenceFoldersProperty, true);
                        so.ApplyModifiedProperties();
                    }
                    EditorGUILayout.Space();

                    /*using (new EditorGUILayout.HorizontalScope())
                    {
                        var content = new GUIContent("Reference depth", "How deep should Easy Save look when gathering references from an object? Higher means deeper.");
                        EditorGUILayout.PrefixLabel(content);
                        editorSettings.collectDependenciesDepth = EditorGUILayout.IntField(editorSettings.collectDependenciesDepth);
                    }*/

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var content = new GUIContent("Reference timeout (seconds)", "How many seconds should Easy Save taking collecting references for an object before timing out?");
                        EditorGUILayout.PrefixLabel(content);
                        editorSettings.collectDependenciesTimeout = EditorGUILayout.IntField(editorSettings.collectDependenciesTimeout);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("Use Global References");

                        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                        bool useGlobalReferences = !symbols.Contains("ES3GLOBAL_DISABLED");
                        if(EditorGUILayout.Toggle(useGlobalReferences) != useGlobalReferences)
                        {
                            // Remove the existing symbol even if we're disabling global references, just incase it's already in there.
                            symbols = symbols.Replace("ES3GLOBAL_DISABLED;", ""); // With semicolon
                            symbols = symbols.Replace("ES3GLOBAL_DISABLED", "");  // Without semicolon

                            // Add the symbol if useGlobalReferences is currently true, meaning that we want to disable it.
                            if (useGlobalReferences)
                                symbols = "ES3GLOBAL_DISABLED;" + symbols;

                            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);

                            if(useGlobalReferences)
                                EditorUtility.DisplayDialog("Global references disabled for build platform", "This will only disable Global References for this build platform. To disable it for other build platforms, open that platform in the Build Settings and uncheck this box again.", "Ok");
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var content = new GUIContent("Add All Prefabs to Manager", "Should all prefabs with ES3Prefab Components be added to the manager?");
                        EditorGUILayout.PrefixLabel(content);
                        editorSettings.addAllPrefabsToManager = EditorGUILayout.Toggle(editorSettings.addAllPrefabsToManager);
                    }

                    EditorGUILayout.Space();
                }
            }

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(editorSettings);

            EditorGUIUtility.labelWidth = labelWidth; // Set the label width back to default
		}

		public void Init()
		{
            editorSettings = ES3Settings.defaultSettingsScriptableObject;
			settings = editorSettings.settings;
            so = new SerializedObject(editorSettings);
            referenceFoldersProperty = so.FindProperty("referenceFolders");
        }
	}

}
