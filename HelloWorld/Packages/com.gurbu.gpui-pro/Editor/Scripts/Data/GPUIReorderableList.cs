// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIReorderableList
    {
        private bool _foldout = false;
        private SerializedObject _serializedObject;
        private SerializedProperty _listProperty;
        private ReorderableList _list;
        private int _selectedIndex;
        private string _header;
        private Func<Rect, SerializedProperty, bool, bool> _drawMethod;
        private Texture2D _selectedBG;
        private Func<bool, float> _elementHeightCallback;
        private Func<int, int> _onSelectionChanged;
        private Func<int> _getSelectedIndex;
        private Action _dataChanged;

        public GPUIReorderableList(SerializedObject serializedObject, string header, Func<Rect, SerializedProperty, bool, bool> drawMethod, Func<bool, float> elementHeightCallback, Func<int, int> onSelectionChanged, Func<int> getSelectedIndex)
        {
            _serializedObject = serializedObject;
            _header = header;
            _selectedIndex = -1;
            _drawMethod = drawMethod;
            _elementHeightCallback = elementHeightCallback;
            _onSelectionChanged = onSelectionChanged;
            _getSelectedIndex = getSelectedIndex;
        }

        public void Draw(SerializedProperty listProperty, Action dataChanged = null)
        {
            _listProperty = listProperty;
            _dataChanged = dataChanged;

            EditorGUILayout.BeginVertical(GPUIEditorConstants.Styles.box);
            _foldout = EditorGUILayout.Foldout(_foldout, _header, true);
            if (_foldout)
            {
                SetSelected(_getSelectedIndex.Invoke());
                _list = new ReorderableList(_serializedObject, _listProperty, false, false, true, true)
                {
                    drawElementCallback = DrawItems,
                    //drawHeaderCallback = DrawHeader,
                    onAddCallback = OnAdd,
                    onRemoveCallback = OnRemove,
                    onSelectCallback = OnSelect,
                    elementHeightCallback = OnElementHeight,
                    drawElementBackgroundCallback = OnBackground,
                };
                _list.DoLayoutList();
            }
            EditorGUILayout.EndVertical();
        }

        public void SetSelected(int index)
        {
            _selectedIndex = index;
        }

        private void OnBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index == _selectedIndex)
            {
                if (_selectedBG == null)
                {
                    _selectedBG = new Texture2D(1, 1);
                    _selectedBG.SetPixel(0, 0, new Color(0.33f, 0.66f, 1f, 0.15f));
                    _selectedBG.Apply();
                }
                GUI.DrawTexture(rect, _selectedBG);
            }
        }

        private float OnElementHeight(int index)
        {
            return _elementHeightCallback.Invoke(index == _selectedIndex);
        }

        private void OnSelect(ReorderableList list)
        {
            SetSelected(_onSelectionChanged.Invoke(list.selectedIndices[0]));
        }

        private void OnAdd(ReorderableList list)
        {
            int index = _listProperty.arraySize;
            _listProperty.InsertArrayElementAtIndex(index);
            _serializedObject.ApplyModifiedProperties();
            _dataChanged?.Invoke();
        }

        private void OnRemove(ReorderableList list)
        {
            if (_selectedIndex >= 0)
            {
                _listProperty.DeleteArrayElementAtIndex(_selectedIndex);
                _serializedObject.ApplyModifiedProperties();
                _selectedIndex = -1;
                _dataChanged?.Invoke();
            }
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, _header);
        }

        private void DrawItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = _listProperty.GetArrayElementAtIndex(index);
            if (_drawMethod.Invoke(rect, element, index == _selectedIndex))
                _selectedIndex = index;
            else if (_selectedIndex == index)
                _selectedIndex = -1;
        }
    }
}
