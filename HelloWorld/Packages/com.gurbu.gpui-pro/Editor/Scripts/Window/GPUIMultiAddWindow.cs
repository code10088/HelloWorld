// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIMultiAddWindow : EditorWindow
    {
        private static GPUIManagerEditor _managerEditor;

        public static void ShowWindow(Vector2 position, GPUIManagerEditor managerEditor)
        {
            EditorWindow window = EditorWindow.GetWindow(typeof(GPUIMultiAddWindow), true, "GPUI Multiple Add", true);
            window.minSize = new Vector2(400, 200);
            window.maxSize = new Vector2(400, 200);
            _managerEditor = managerEditor;
            ActiveEditorTracker.sharedTracker.isLocked = true;
        }

        void OnGUI()
        {
            if (_managerEditor == null || _managerEditor.GetManager() == null)
            {
                Close();
                return;
            }

            Rect buttonRect = GUILayoutUtility.GetRect(360, 180, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GPUIEditorUtility.DrawColoredButton(new GUIContent("<size=32>↓</size><size=18>Drop Files Here</size>"), Color.clear,
                Color.white,
                FontStyle.Bold, buttonRect,
                null,
                true, true,
                (o) =>
                {
                    _managerEditor.AddPickerObject(o);
                });
        }

        private void OnDisable()
        {
            ActiveEditorTracker.sharedTracker.isLocked = false;
            if (_managerEditor != null && _managerEditor.GetManager() != null)
                Selection.activeGameObject = _managerEditor.GetManager().gameObject;
        }
    }
}