// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro
{
    public static class GPUIEditorTextUtility
    {
        public static Dictionary<int, GPUIText> textDictionary;
        public static GPUIText EMPTY_GPUI_TEXT = new GPUIText("", "");

        public static bool TryGetGPUIText(string textCode, out GPUIText gpuiText)
        {
            gpuiText = EMPTY_GPUI_TEXT;
            if (!string.IsNullOrEmpty(textCode) && textCode != "data")
            {
                if (textCode.StartsWith("_"))
                    textCode = textCode.Substring(1);
                if (textDictionary == null || textDictionary.Count == 0)
                    ReadTextFile();
                if (textDictionary.TryGetValue(textCode.GetHashCode(), out gpuiText))
                    return true;
                else
                {
#if GPUIPRO_DEVMODE
                    Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Can not find editor text for textCode: " + textCode + ". Adding automatically.");
#endif
                    gpuiText = new(textCode, GPUIUtility.CamelToTitleCase(textCode));
                    textDictionary.Add(textCode.GetHashCode(), gpuiText);
                    return true;
                }
            }
            return false;
        }

        public static GUIContent GetGUIContent(string textCode)
        {
            GUIContent result = new GUIContent(textCode);
            if (TryGetGPUIText(textCode, out GPUIText gpuiText))
            {
                result.text = gpuiText.title;
                result.tooltip = gpuiText.tooltip;
            }
            return result;
        }

#if GPUIPRO_DEVMODE
        [MenuItem("Tools/GPU Instancer Pro/Development/Read Editor Text", validate = false, priority = 9999)]
#endif
        public static void ReadTextFile()
        {
            textDictionary = new Dictionary<int, GPUIText>();

            ReadTextAsset(GPUIEditorConstants.GetEditorTextPath(), null);

            if (GPUIEditorSettings.Instance._subModuleVersions == null)
                return;

            for (int i = 0; i < GPUIEditorSettings.Instance._subModuleVersions.Count; i++)
            {
                GPUIEditorSettings.GPUISubModuleVersion gpuiSubModule = GPUIEditorSettings.Instance._subModuleVersions[i];
                ReadTextAsset(gpuiSubModule.GetEditorTextPath(), gpuiSubModule.packageName);
            }
        }


        private static void ReadTextAsset(string assetPath, string packageName)
        {
            int lineCount = 0;
            if (!File.Exists(assetPath))
                return;
            using (StreamReader sr = new StreamReader(assetPath, Encoding.UTF8))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    lineCount++;
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] split = line.Split('|');
                        if (split.Length >= 4)
                        {
                            int code = split[0].GetHashCode();
                            if (!textDictionary.ContainsKey(code))
                            {
                                GPUIText gpuiText = new GPUIText(split);
                                gpuiText.packageName = packageName;
                                textDictionary.Add(code, gpuiText);
#if GPUIPRO_DEVMODE
                                gpuiText.IsValid();
#endif
                            }
#if GPUIPRO_DEVMODE
                            else
                                Debug.LogError(GPUIConstants.LOG_PREFIX + "Duplicate codeText: " + split[0]);
#endif
                        }
#if GPUIPRO_DEVMODE
                        else
                            Debug.LogError(GPUIConstants.LOG_PREFIX + "Incorrect split at line " + lineCount);
#endif
                    }
                }
            }
        }

#if GPUIPRO_DEVMODE
        public static void SaveTextValues()
        {
            SaveTextValues(GPUIEditorConstants.GetEditorTextPath(), null);

            if (GPUIEditorSettings.Instance._subModuleVersions == null)
                return;

            for (int i = 0; i < GPUIEditorSettings.Instance._subModuleVersions.Count; i++)
            {
                GPUIEditorSettings.GPUISubModuleVersion gpuiSubModule = GPUIEditorSettings.Instance._subModuleVersions[i];
                SaveTextValues(gpuiSubModule.GetEditorTextPath(), gpuiSubModule.packageName);
            }
        }

        private static void SaveTextValues(string assetPath, string packageName)
        {
            if (!File.Exists(assetPath))
                return;
            bool isNullPackage = string.IsNullOrEmpty(packageName);
            using (StreamWriter sw = new(assetPath, false, Encoding.UTF8))
            {
                foreach (GPUIText gpuiText in textDictionary.Values)
                {
                    if (isNullPackage)
                    {
                        if (string.IsNullOrEmpty(gpuiText.packageName))
                            sw.WriteLine(gpuiText.ToString());
                    }
                    else if (packageName.Equals(gpuiText.packageName))
                        sw.WriteLine(gpuiText.ToString());
                }
            }
        }
#endif

        [Serializable]
        public class GPUIText
        {
            public string codeText;
            public string title;
            public string tooltip;
            public string helpText;
            public string wwwAddress;
            public string packageName;

            public string HelpTextNoEscape
            {
                get
                {
                    if (helpText == null)
                        return null;
                    return helpText.Replace("\\n", "\n");
                }
            }

            public string TooltipNoEscape
            {
                get
                {
                    if (tooltip == null)
                        return null;
                    return tooltip.Replace("\\n", "\n");
                }
            }

            public GPUIText(string codeText, string title)
            {
                this.codeText = codeText;
                this.title = title;
                this.tooltip = title;
                this.helpText = null;
            }

            public GPUIText(string[] split)
            {
                codeText = split[0];
                title = split[1];
                if (split.Length > 2)
                    tooltip = split[2].Replace("\\n", "\n");
                if (split.Length > 3)
                    helpText = split[3].Replace("\\n", "\n");
                if (split.Length > 4)
                    wwwAddress = split[4];
            }

            public GUIContent GetGUIContent()
            {
                return new GUIContent(title, tooltip);
            }

            public bool IsValid()
            {
#if GPUIPRO_DEVMODE
                if (string.IsNullOrEmpty(codeText))
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Invalid codeText!");
                    return false;
                }
                //if (string.IsNullOrEmpty(title))
                //{
                //    Debug.LogError(GPUIConstants.LOG_PREFIX + "Invalid title for: " + codeText);
                //    return false;
                //}
                //if (string.IsNullOrEmpty(tooltip))
                //{
                //    Debug.LogError(GPUIConstants.LOG_PREFIX + "Invalid tooltip for: " + codeText);
                //    return false;
                //}
                // Allow not having helpText
                //if (string.IsNullOrEmpty(helpText))
                //{
                //    Debug.LogError(GPUIConstants.LOG_PREFIX + "Invalid helpText for: " + codeText);
                //    return false;
                //}
#endif
                return true;
            }

            public override string ToString()
            {
                return codeText + "|" 
                    + title + "|" 
                    + (tooltip == null ? "" : Regex.Replace(tooltip, @"\r\n?|\n", "\\n")) + "|" 
                    + (helpText == null ? "" : Regex.Replace(helpText, @"\r\n?|\n", "\\n")) + "|" 
                    + (wwwAddress == null ? "" : wwwAddress);
            }
        }
    }
}