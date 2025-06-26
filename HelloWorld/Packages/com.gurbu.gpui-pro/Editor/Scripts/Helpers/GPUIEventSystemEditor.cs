// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUIEventSystem))]
    public class GPUIEventSystemEditor : GPUIEditor
    {
        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#GPUI_Event_System";
        }

        public override string GetTitleText()
        {
            return "GPUI Event System";
        }
    }
}
