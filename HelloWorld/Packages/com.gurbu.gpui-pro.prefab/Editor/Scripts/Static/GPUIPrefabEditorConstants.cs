// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    public static class GPUIPrefabEditorConstants
    {
        private static readonly string FILE_MATERIAL_VARIATIONS_INCLUDE_HLSL_TEMPLATE = "Packages/com.gurbu.gpui-pro.prefab/Editor/Extras/GPUIMaterialVariationsIncludeHLSLTemplate.txt";
        private static readonly string FILE_MATERIAL_VARIATIONS_SUB_GRAPH_TEMPLATE = "Packages/com.gurbu.gpui-pro.prefab/Editor/Extras/GPUIMaterialVariationsSubGraphTemplate.txt";

        public static readonly string PLACEHOLDER_VariationBufferName = "{VariationBufferName}";
        public static readonly string PLACEHOLDER_VariationSetupCode = "{VariationSetupCode}";
        public static readonly string PLACEHOLDER_IncludeFileGUID = "{IncludeFileGUID}";
        public static readonly string PLACEHOLDER_MaterialVariationKeyword = "{MaterialVariationKeyword}";

        public static string MaterialVariationsHLSLTemplate => GPUIUtility.ReadTextFileAtPath(FILE_MATERIAL_VARIATIONS_INCLUDE_HLSL_TEMPLATE);

        public static string MaterialVariationsSubGraphTemplate => GPUIUtility.ReadTextFileAtPath(FILE_MATERIAL_VARIATIONS_SUB_GRAPH_TEMPLATE);
    }
}
