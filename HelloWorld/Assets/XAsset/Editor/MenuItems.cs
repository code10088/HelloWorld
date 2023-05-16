using System.IO;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    public static class MenuItems
    {
        private const string kSimulationMode = "xasset/Simulation Mode";
        private const string kUpdatable = "xasset/Updatable";


        [MenuItem("xasset/About xasset", false, 1)]
        public static void OpenAbout()
        {
            Application.OpenURL("https://xasset.cc");
        }

        [MenuItem("xasset/Check for Updates", false, 1)]
        public static void CheckForUpdates()
        {
            Application.OpenURL("https://xasset.cc/docs/change-log");
        }

        [MenuItem("xasset/Get Unity Online Services", false, 1)]
        public static void GetUnityOnlineServices()
        {
            Application.OpenURL("https://uos.unity.cn");
        }

        [MenuItem("xasset/Settings", false, 80)]
        public static void PingSettings()
        {
            Selection.activeObject = Settings.GetDefaultSettings();
            EditorGUIUtility.PingObject(Selection.activeObject);
            EditorUtility.FocusProjectWindow();
        }

        [MenuItem(kSimulationMode, false, 80)]
        public static void SwitchSimulationMode()
        {
            var settings = Settings.GetDefaultSettings();
            settings.player.simulationMode = !settings.player.simulationMode;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        [MenuItem(kSimulationMode, true, 80)]
        public static bool RefreshSimulationMode()
        {
            var settings = Settings.GetDefaultSettings();
            Menu.SetChecked(kSimulationMode, settings.player.simulationMode);
            return true;
        }

        [MenuItem(kUpdatable, false, 80)]
        public static void SwitchUpdateEnabled()
        {
            var settings = Settings.GetDefaultSettings();
            settings.player.updatable = !settings.player.updatable;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        [MenuItem(kUpdatable, true, 80)]
        public static bool RefreshUpdateEnabled()
        {
            var settings = Settings.GetDefaultSettings();
            Menu.SetChecked(kUpdatable, settings.player.updatable);
            return true;
        }

        [MenuItem("xasset/Build Bundles", false, 100)]
        public static void BuildBundles()
        {
            Builder.BuildBundles(Selection.GetFiltered<Build>(SelectionMode.DeepAssets));
        }

        [MenuItem("xasset/Build Bundles with Last Build", false, 100)]
        public static void BuildBundlesWithLastBuild()
        {
            Builder.BuildBundlesWithLastBuild(Selection.GetFiltered<Build>(SelectionMode.DeepAssets));
        }

        [MenuItem("xasset/Build Player", false, 100)]
        public static void BuildPlayer()
        {
            editor.BuildPlayer.Build();
        }

        [MenuItem("xasset/Build Player Assets", false, 100)]
        public static void BuildPlayerAssetsWithSelection()
        {
            var path = EditorUtility.OpenFilePanelWithFilters("Select", Settings.PlatformDataPath,
                new[] { "versions", "json" });
            if (string.IsNullOrEmpty(path)) return;
            var versions = Utility.LoadFromFile<Versions>(path);
            BuildPlayerAssets.CustomBuilder = null;
            BuildPlayerAssets.StartNew(versions);
        }

        [MenuItem("xasset/Build Update Info", false, 100)]
        public static void BuildUpdateInfo()
        {
            var path = EditorUtility.OpenFilePanelWithFilters("Select", Settings.PlatformDataPath,
                new[] { "versions", "json" });
            if (string.IsNullOrEmpty(path)) return;

            var versions = Utility.LoadFromFile<Versions>(path);
            var file = new FileInfo(path);
            var hash = Utility.ComputeHash(path);
            Builder.BuildUpdateInfo(versions, hash, file.Length);
        }

        [MenuItem("xasset/Print Changes with Selection", false, 150)]
        public static void PrintChangesFromSelection()
        {
            var path = EditorUtility.OpenFilePanelWithFilters("Select", Settings.PlatformDataPath,
                new[] { "versions", "json" });
            if (string.IsNullOrEmpty(path)) return;
            var versions = Utility.LoadFromFile<Versions>(path);
            var filename = versions.GetFilename();
            var records = Utility.LoadFromFile<Changes>(Settings.GetCachePath(Changes.Filename));
            if (records.TryGetValue(filename, out var value)) Builder.GetChanges(value.changes, filename);
        }

        [MenuItem("xasset/Clear Download", false, 200)]
        public static void ClearDownload()
        {
            var directory = Application.persistentDataPath;
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            PlayerPrefs.SetString(Assets.kBundlesVersions, string.Empty);
            PlayerPrefs.Save();
        }

        [MenuItem("xasset/Clear Bundles", false, 200)]
        public static void ClearBundles()
        {
            var directories = new[] { Settings.PlatformDataPath, Settings.PlatformCachePath };
            foreach (var directory in directories)
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
        }

        [MenuItem("xasset/Clear History", false, 200)]
        public static void ClearHistory()
        {
            ClearBuildHistory.Start();
        }

        [MenuItem("xasset/Open/Download Data Path", false, 300)]
        public static void OpenDownloadBundles()
        {
            EditorUtility.OpenWithDefaultApp(Assets.DownloadDataPath);
        }

        [MenuItem("xasset/Open/Temp Data Path", false, 300)]
        public static void OpenTempDataPath()
        {
            EditorUtility.OpenWithDefaultApp(Application.temporaryCachePath);
        }

        [MenuItem("Assets/To Json")]
        public static void ToJson()
        {
            var activeObject = Selection.activeObject;
            var json = JsonUtility.ToJson(activeObject);
            var path = AssetDatabase.GetAssetPath(activeObject);
            var ext = Path.GetExtension(path);
            File.WriteAllText(path.Replace(ext, ".json"), json);
        }
    }
}