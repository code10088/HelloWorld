// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    public interface IGPUIEditor
    {
        public abstract void DrawContentGUI(VisualElement contentElement);
        public abstract string GetTitleText();
        public abstract string GetWikiURLParams();
    }
}
