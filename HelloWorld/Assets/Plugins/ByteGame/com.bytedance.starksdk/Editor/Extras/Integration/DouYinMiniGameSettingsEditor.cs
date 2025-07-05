#if TUANJIE_1_5_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace TTSDK.Tool
{
    class DouYinMiniGameSettingsEditor : MiniGameSettingsEditor
    {
        private readonly Dictionary<string, string> _editingInputData = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> _editingBooleanData = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _editingEnumData = new Dictionary<string, int>();
        
        private bool _isFoldInfoOptions = true;
        private bool _isFoldLaunchOptions = true;
        private bool _isFoldPreloadOptions = true;
        private bool _isFoldCompileOptions = true;

        private static int _textareaHeight = 50;
        private static float _labelWidth = 140;
        private static float _toggleWidth = 16;
        private static float _snapPadding = 10;
        private static float _fieldWidth => EditorGUIUtility.currentViewWidth - (_labelWidth + _snapPadding * 2 + 35);
        
        
        public override void OnMiniGameSettingsIMGUI(SerializedObject serializedObject, SerializedProperty miniGameProperty)
        {
            ReadSettingsProperties(miniGameProperty);
            serializedObject.UpdateIfRequiredOrScript();
            
            DrawDouYinSettingsGUI(serializedObject, miniGameProperty);
            
            SaveSettingsModifiedProperties(miniGameProperty);
            serializedObject.ApplyModifiedProperties();
        }
        
        #region Style
        
        private void DrawDouYinSettingsGUI(SerializedObject serializedObject, SerializedProperty miniGameProperty)
        {
            _isFoldInfoOptions = EditorGUILayout.Foldout(_isFoldInfoOptions, "基本信息");
            if (_isFoldInfoOptions)
            {
                EditorGUILayout.BeginVertical("frameBox", GUILayout.ExpandWidth(true));

                CreateInput("appId", "游戏 AppID");
                CreateEnumPopup("orientation", "游戏方向", new[] { "Portrait", "Landscape" }, new[] { 0, 1 });
                CreateInput("wasmMemorySize", "UnityHeap 内存 (?)", "UnityHeap预留内存大小。根据游戏而定，默认128M.调试期间开启Profiler时，TotalMemory的值即代表此值，此值的设定遵循以下原则：\n1.请勿设置过小或过大，尽量不低于32或大于2048，否则游戏运行会有问题。\n2.运行完所有场景期间都不触发TotalMemory的增长，\n3.此值不宜设置过大，越大代表能运行的玩家所少，越容易出现内存不足的问题。\n因此如果TotalMemory有增长，那么设置一个稍大于TotalMemory的整数即可");

                EditorGUILayout.EndVertical();
            }
            
            _isFoldLaunchOptions = EditorGUILayout.Foldout(_isFoldLaunchOptions, "启动加载配置");
            if (_isFoldLaunchOptions)
            {
                EditorGUILayout.BeginVertical("frameBox", GUILayout.ExpandWidth(true));
                
                CreateBoolean("needCompress", "压缩首包资源");
                CreateTextarea("urlCacheList", "缓存资源域名 (?)", "配置的域名下资源会使用缓存\n一个域名占一行\n示例: douyin.com");
                CreateTextarea("dontCacheFileNames", "不缓存的文件 (?)", "配置的文件名每次都会重新下载，不会缓存\n一个文件名一行，可填写后缀名匹配，不支持通配符。\n示例: json");

                EditorGUILayout.EndVertical();
            }
            
            _isFoldPreloadOptions = EditorGUILayout.Foldout(_isFoldPreloadOptions, new UnityEngine.GUIContent("预下载配置 (Experimental)", "小游戏提供游戏启动前以及游戏启动后对指定资源预下载的能力，提升游戏加载速度。"));
            if (_isFoldPreloadOptions)
            {
                EditorGUILayout.BeginVertical("frameBox", GUILayout.ExpandWidth(true));
                
                CreateInput("preloadFiles", "文件列表 (?)", "使用;间隔，支持模糊匹配");
                CreateInput("CDN", "游戏资源 CDN", "资源部署 CDN 的前缀，仅用于预下载");
                CreateInput("preloadDataListUrl", "动态资源列表接口", "开发者自行实现的接口，其接口返回结构需满足预下载能力要求");
                
                EditorGUILayout.EndVertical();
            }
            
            _isFoldCompileOptions = EditorGUILayout.Foldout(_isFoldCompileOptions, "调试编译选项");
            if (_isFoldCompileOptions)
            {
                EditorGUILayout.BeginVertical("frameBox", GUILayout.ExpandWidth(false));

                var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.MiniGame);
                _editingBooleanData["isWebGL2"] = graphicsAPIs.Length > 0 && graphicsAPIs[0] == GraphicsDeviceType.OpenGLES3;
                CreateBoolean("isWebGL2", "WebGL2.0 (beta)",  null, true, "Managed via PlayerSettings", "WebGL2.0 配置现在由引擎管理，请在 Player Settings 中配置 Graphics APIs，此处不再生效");
                CreateBoolean("clearStreamingAssets", "Clear Streaming Assets", "构建时移除产物中 StreamingAssets 目录下的文件");
                CreateBoolean("iOSPerformancePlus", "开启高性能+模式", "针对iOS系统开启高性能+模式，降低游戏内存压力、提升渲染兼容性和效率", false, null, null,
                    () =>
                    {
                        EditorGUILayout.LabelField(string.Empty, GUILayout.Width(_snapPadding));
                        if (GUILayout.Button("什么是高性能+模式?", EditorStyles.linkLabel))
                        {
                            Application.OpenURL("https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/performance-optimization/high-performance-plus-mode");
                        }
                        EditorGUILayout.LabelField(string.Empty, GUILayout.Width(_snapPadding));
                    });
                CreateBoolean("profiling", "显示性能面板");
                
                EditorGUILayout.EndVertical();
            }
            
        }

        /// <summary>
        /// 创建配置项标签
        /// </summary>
        private void CreateFieldLabel(string label, string tooltip = null)
        {
            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(_snapPadding));
            if (tooltip == null)
            {
                GUILayout.Label(label, GUILayout.Width(_labelWidth));
            }
            else
            {
                GUILayout.Label(new GUIContent(label, tooltip), GUILayout.Width(_labelWidth));
            }
        }

        /// <summary>
        /// 创建单行输入控件
        /// </summary>
        private void CreateInput(string name, string label, string tooltip = null)
        {
            if (!_editingInputData.TryGetValue(name, out var value))
                _editingInputData[name] = value = "";
            
            GUILayout.BeginHorizontal();
            CreateFieldLabel(label, tooltip);
            _editingInputData[name] = GUILayout.TextField(value, GUILayout.MaxWidth(_fieldWidth));
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 创建多行输入控件
        /// </summary>
        private void CreateTextarea(string name, string label, string tooltip = null)
        {
            if (!_editingInputData.TryGetValue(name, out var value))
                _editingInputData[name] = value = "";
            
            GUILayout.BeginHorizontal();
            CreateFieldLabel(label, tooltip);
            EditorGUI.BeginDisabledGroup(false);
            _editingInputData[name] = EditorGUILayout.TextArea(value,
                GUILayout.MaxWidth(_fieldWidth), GUILayout.MinHeight(_textareaHeight));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
        }


        /// <summary>
        /// 创建布尔型复选框控件
        /// </summary>
        private void CreateBoolean(string name, string label, string tooltip = null, bool disabled = false, string disabledLabel = null, string disabledTooltip = null, Action createAction = null)
        {
            if (!_editingBooleanData.TryGetValue(name, out var value))
                _editingBooleanData[name] = value = false;
            
            GUILayout.BeginHorizontal();
            CreateFieldLabel(label, tooltip);
            EditorGUI.BeginDisabledGroup(disabled);
            _editingBooleanData[name] = EditorGUILayout.Toggle(value, GUILayout.Width(_toggleWidth));
            if (disabled && disabledLabel != null)
            {
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(_snapPadding));
                if (disabledTooltip == null)
                {
                    GUILayout.Label(disabledLabel);
                }
                else
                {
                    GUILayout.Label(new UnityEngine.GUIContent(disabledLabel, disabledTooltip));
                }
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(_snapPadding));
            }
            
            if (createAction != null)
                createAction();
            
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 创建枚举下拉选框控件
        /// </summary>
        private void CreateEnumPopup(string name, string label, string[] options, int[] values, string tooltip = null)
        {
            if (!_editingEnumData.TryGetValue(name, out var value))
                _editingEnumData[name] = value = 0;
            
            GUILayout.BeginHorizontal();
            CreateFieldLabel(label, tooltip);
            _editingEnumData[name] = EditorGUILayout.IntPopup(value, options, values, GUILayout.MaxWidth(_fieldWidth));
            GUILayout.EndHorizontal();
        }
        
        #endregion
        
        #region Data
        
        /// <summary>
        /// 从配置文件读取所有属性
        /// </summary>
        /// <param name="miniGameProperty"></param>
        private void ReadSettingsProperties(SerializedProperty miniGameProperty)
        {
            _editingInputData["appId"] = ReadProperty<string>(miniGameProperty, "appId");
            _editingInputData["wasmMemorySize"] = ReadProperty<int>(miniGameProperty, "wasmMemorySize").ToString();
            
            
            _editingInputData["CDN"] = ReadProperty<string>(miniGameProperty, "CDN");
            _editingInputData["preloadFiles"] = ReadProperty<string>(miniGameProperty, "preloadFiles");
            _editingInputData["preloadDataListUrl"] = ReadProperty<string>(miniGameProperty, "preloadDataListUrl");
            
            _editingInputData["urlCacheList"] = ReadProperty<string>(miniGameProperty, "urlCacheList");
            _editingInputData["dontCacheFileNames"] = ReadProperty<string>(miniGameProperty, "dontCacheFileNames");

            _editingBooleanData["needCompress"] = ReadProperty<bool>(miniGameProperty, "needCompress");
            _editingBooleanData["iOSPerformancePlus"] = ReadProperty<bool>(miniGameProperty, "iOSPerformancePlus");
            _editingBooleanData["profiling"] = ReadProperty<bool>(miniGameProperty, "profiling");
            _editingBooleanData["clearStreamingAssets"] = ReadProperty<bool>(miniGameProperty, "clearStreamingAssets");

            _editingEnumData["orientation"] = ReadProperty<int>(miniGameProperty, "orientation");
        }
        
        /// <summary>
        /// 保存所有属性到配置文件
        /// </summary>
        /// <param name="miniGameProperty"></param>
        private void SaveSettingsModifiedProperties(SerializedProperty miniGameProperty)
        {
            SaveProperty(miniGameProperty, "appId", _editingInputData["appId"]);
            SaveProperty(miniGameProperty, "wasmMemorySize", int.Parse(_editingInputData["wasmMemorySize"]));
            
            SaveProperty(miniGameProperty, "CDN", _editingInputData["CDN"]);
            SaveProperty(miniGameProperty, "preloadFiles", _editingInputData["preloadFiles"]);
            SaveProperty(miniGameProperty, "preloadDataListUrl", _editingInputData["preloadDataListUrl"]);
            
            SaveProperty(miniGameProperty, "urlCacheList", _editingInputData["urlCacheList"]);
            SaveProperty(miniGameProperty, "dontCacheFileNames", _editingInputData["dontCacheFileNames"]);
            
            SaveProperty(miniGameProperty, "needCompress", _editingBooleanData["needCompress"]);
            SaveProperty(miniGameProperty, "iOSPerformancePlus", _editingBooleanData["iOSPerformancePlus"]);
            SaveProperty(miniGameProperty, "profiling", _editingBooleanData["profiling"]);
            SaveProperty(miniGameProperty, "clearStreamingAssets", _editingBooleanData["clearStreamingAssets"]);
            
            SaveProperty(miniGameProperty, "orientation", _editingEnumData["orientation"]);
        }

        /// <summary>
        /// 从配置文件读入属性字段
        /// </summary>
        private static T ReadProperty<T>(SerializedProperty miniGameProperty, string propertyName)
        {
            var property = miniGameProperty.FindPropertyRelative(propertyName);
            if (property == null)
                return default;
            
            var rt = typeof(T);
            
            if (property.isArray && property.arrayElementType == "string")
            {
                var arr = new string[property.arraySize];
                for (var i = 0; i < property.arraySize; ++i)
                {
                    arr[i] = property.GetArrayElementAtIndex(i).stringValue;
                }

                return (T)(object)string.Join("\n", arr);
            }

            if (rt == typeof(bool))
            {
                return (T)(object)property.boolValue;
            }
            
            if (rt == typeof(int))
            {
                return (T)(object)property.intValue;
            }
            
            if (rt == typeof(float))
            {
                return (T)(object)property.floatValue;
            }
            
            if (rt == typeof(string))
            {
                return (T)(object)property.stringValue;
            }
            
            throw new Exception($"Unsupported property type with ReadProperty<{typeof(T).FullName}>.");
        }

        /// <summary>
        /// 保存属性字段值到配置文件
        /// </summary>
        private static void SaveProperty<T>(SerializedProperty miniGameProperty, string propertyName, T value)
        {
            var property = miniGameProperty.FindPropertyRelative(propertyName);
            var rt = typeof(T);
            
            if (property.isArray && property.arrayElementType == "string")
            {
                var raw = (string)(object)value;
                var arr = raw.Split('\n');
                property.arraySize = arr.Length;
                for (var i = 0; i < property.arraySize; ++i)
                {
                    var elm = property.GetArrayElementAtIndex(i);
                    elm.stringValue = arr[i];
                }
            }
            else if (rt == typeof(bool))
            {
                property.boolValue = (bool)(object)value;
            }
            else if (rt == typeof(int))
            {
                property.intValue = (int)(object)value;
            }
            else if (rt == typeof(float))
            {
                property.floatValue = (float)(object)value;
            }
            else if (rt == typeof(string))
            {
                property.stringValue = (string)(object)value;
            }
            else
            {
                throw new Exception($"Unsupported property type with SaveProperty<{typeof(T).FullName}>.");
            }
        }
        
        #endregion
    }
}
#endif