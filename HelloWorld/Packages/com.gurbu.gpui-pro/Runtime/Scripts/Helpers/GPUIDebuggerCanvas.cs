// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GPUInstancerPro
{
    public class GPUIDebuggerCanvas : MonoBehaviour
    {
        public Toggle showVisibilityToggle;
        public RectTransform contentTransform;

        private Dictionary<int, DebugUIData> _rsgUIs;
        private Action<GPUIDataBuffer<GPUIVisibilityData>> _callback;

        private static readonly float START_Y = -25;
        private static readonly float SPACING_Y = 50;
        private static readonly float HEIGHT = 45;
        private static readonly float WIDTH = 380;
        private static readonly int FONT_SIZE = 18;

        private void OnEnable()
        {
            _rsgUIs = new();
            _callback = VisibilityCallback;
        }

        private void OnDisable()
        {
            if (_rsgUIs != null)
            {
                foreach (var ui in _rsgUIs.Values)
                {
                    if (ui != null)
                        Destroy(ui.uiGO);
                }
                _rsgUIs = null;
            }
        }

        private void Update()
        {
            if (!GPUIRenderingSystem.IsActive)
                return;

            if (contentTransform != null)
            {
                bool isShowVisibility = showVisibilityToggle != null && showVisibilityToggle.isOn;
                foreach (var rsgKV in GPUIRenderingSystem.Instance.RenderSourceGroupProvider)
                {
                    if (!_rsgUIs.ContainsKey(rsgKV.Key))
                    {
                        GameObject uiGO = new GameObject(rsgKV.Value.Name);
                        uiGO.transform.parent = contentTransform;
                        uiGO.transform.localScale = Vector3.one;

                        Text text = uiGO.AddComponent<Text>();
                        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        text.fontSize = FONT_SIZE;
                        text.resizeTextForBestFit = true;
                        text.resizeTextMaxSize = FONT_SIZE;
                        text.resizeTextMinSize = 1;
                        text.color = Color.white;
                        DebugUIData debugUIData = new(uiGO, text, rsgKV.Value.Name, rsgKV.Value.LODGroupData.Length);

                        RectTransform rt = uiGO.GetComponent<RectTransform>();
                        rt.anchorMin = Vector2.up;
                        rt.anchorMax = Vector2.one;
                        rt.anchoredPosition = new Vector2(0, START_Y - (SPACING_Y * _rsgUIs.Count));
                        rt.sizeDelta = new Vector2(WIDTH, HEIGHT);
                        rt.offsetMin = new Vector2(10, rt.offsetMin.y);
                        rt.offsetMax = new Vector2(10, rt.offsetMax.y);

                        _rsgUIs.Add(rsgKV.Key, debugUIData);
                    }
                }

                foreach (var rsgUIKV in _rsgUIs)
                {
                    if (GPUIRenderingSystem.Instance.RenderSourceGroupProvider.TryGetData(rsgUIKV.Key, out var rsg))
                    {
                        rsgUIKV.Value.UpdateText(rsg.InstanceCount, isShowVisibility);
                    }
                }

                contentTransform.sizeDelta = new Vector2(0, SPACING_Y * _rsgUIs.Count);

                if (isShowVisibility)
                {
                    GPUICameraData cameraData = GPUIRenderingSystem.Instance.CameraDataProvider.GetFirstValue();
                    if (cameraData != null && cameraData._visibilityBuffer != null)
                    {
                        cameraData._visibilityBuffer.AsyncDataRequest(_callback, false);
                    }
                }
            }
        }

        private void VisibilityCallback(GPUIDataBuffer<GPUIVisibilityData> buffer)
        {
            NativeArray<GPUIVisibilityData> requestedData = buffer.GetRequestedData();
            if (!requestedData.IsCreated)
                return;
            GPUIVisibilityBuffer visibilityBuffer = buffer as GPUIVisibilityBuffer;
            foreach (var rsgUIKV in _rsgUIs)
            {
                if (visibilityBuffer.cameraData.TryGetVisibilityBufferIndex(rsgUIKV.Key, out int visibilityBufferIndex) && requestedData.Length > visibilityBufferIndex)
                {
                    for (int i = 0; i < rsgUIKV.Value.lodCount; i++)
                    {
                        rsgUIKV.Value.visibleCounts[i] = (int)requestedData[visibilityBufferIndex + i].visibleCount;
                    }
                }
            }
        }

        private class DebugUIData
        {
            public GameObject uiGO;
            public Text text;
            public string name;
            private int _instanceCount;
            public int lodCount;
            public int[] visibleCounts;

            private string _text;
            private bool _isShowVisibility;

            public DebugUIData(GameObject uiGO, Text text, string name, int lodCount)
            {
                this.uiGO = uiGO;
                this.text = text;
                this.name = name;
                text.text = name;
                this.lodCount = lodCount;
                visibleCounts = new int[8];
                _instanceCount = -1;
            }

            public void UpdateText(int instanceCount, bool isShowVisibility)
            {
                if (_instanceCount == instanceCount && !_isShowVisibility && _isShowVisibility == isShowVisibility)
                    return;
                _isShowVisibility = isShowVisibility;
                _instanceCount = instanceCount;
                _text = "<b>" + name + "</b>\nIC: " + _instanceCount;
                if (_isShowVisibility)
                {
                    for (int i = 0; i < lodCount; i++)
                        _text += "    LOD" + i + ": " + visibleCounts[i];
                }
                text.text = _text;
            }
        }
    }
}
