// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro.PrefabModule
{
    public static class GPUIPrefabConstants
    {
        #region Paths & File Names

        private static string _packagesPath;
        public static string GetPackagesPath()
        {
            if (string.IsNullOrEmpty(_packagesPath))
                _packagesPath = "Packages/com.gurbu.gpui-pro.prefab/";
            return _packagesPath;
        }

        #endregion Paths & File Names

        #region Shaders
        public static readonly string Kw_GPUI_MATERIAL_VARIATION = "GPUI_MATERIAL_VARIATION";
        #endregion Shaders
    }
}