// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    public abstract class GPUIPreviewSceneWindow : GPUIEditorWindow
    {
        private Scene _previewScene;
        private string _previousScenePath;
        private Vector3 _previousScenePivot;
        private Quaternion _previousSceneRotation;
        private float _previousSceneSize;
        private GameObject _previewWalls;

        protected GameObject _previewGO;
        protected string _errorText;
        protected bool _showWalls = true;

        private void OnDestroy()
        {
            Dispose();
            ReturnToPreviousScene();
        }

        protected virtual void Initialize()
        {
            _previousScenePath = SceneManager.GetActiveScene().path;
            SaveSceneView(SceneView.lastActiveSceneView);

            Scene previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(previewScene);
            GameObject sceneElements = (GameObject)PrefabUtility.InstantiatePrefab(GPUIConstants.PreviewSceneElements, previewScene);
            _previewWalls = sceneElements.transform.Find("CalibrationWalls").gameObject;

            Selection.activeObject = GeneratePreview();
            SetSceneView(SceneView.lastActiveSceneView);
        }

        protected virtual void Dispose()
        {
            if (_previewGO != null)
                DestroyImmediate(_previewGO);
            _previewGO = null;
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            contentElement.Add(new IMGUIContainer(() => DrawIMGUIContents()));
        }

        private void DrawIMGUIContents()
        {
            EditorGUILayout.Space(10);
            EditorGUI.BeginChangeCheck();

            DrawContents();

            if (EditorGUI.EndChangeCheck())
                GeneratePreview();

            if (_previewGO != null)
            {
                EditorGUILayout.Space(10);
                EditorGUI.BeginChangeCheck();
                _showWalls = EditorGUILayout.Toggle("Show Calibration Walls", _showWalls);
                if (EditorGUI.EndChangeCheck())
                    _previewWalls.SetActive(_showWalls);

                EditorGUILayout.BeginHorizontal();
                GPUIEditorUtility.DrawColoredButton(new GUIContent("Save as Asset"), GPUIEditorConstants.Colors.lightGreen, Color.white, FontStyle.Bold, Rect.zero, OnSave);
                EditorGUILayout.Space(10);
                DrawAdditionalButtons();
                GPUIEditorUtility.DrawColoredButton(new GUIContent("Close and Return"), GPUIEditorConstants.Colors.darkRed, Color.white, FontStyle.Bold, Rect.zero, Close);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(10);
            }
            else
            {
                GPUIEditorUtility.DrawColoredButton(new GUIContent("Close and Return"), GPUIEditorConstants.Colors.darkRed, Color.white, FontStyle.Bold, Rect.zero, Close);
            }


            if (!string.IsNullOrEmpty(_errorText))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox(_errorText, MessageType.Error);
                EditorGUILayout.Space(10);
            }
        }

        protected virtual void DrawAdditionalButtons() { }

        protected abstract void DrawContents();

        protected abstract GameObject GeneratePreview();
        protected abstract void OnSave();

        private void SaveSceneView(SceneView sceneView)
        {
            _previousScenePivot = sceneView.pivot;
            _previousSceneRotation = sceneView.rotation;
            _previousSceneSize = sceneView.size;
        }

        protected virtual void SetSceneView(SceneView sceneView)
        {
            sceneView.size = 1;
            sceneView.rotation = Quaternion.Euler(25, 0, 0);
            sceneView.pivot = new Vector3(0, 1f, -1f);
            sceneView.Repaint();
        }

        private void LoadSceneView(SceneView sceneView)
        {
            sceneView.pivot = _previousScenePivot;
            sceneView.rotation = _previousSceneRotation;
            sceneView.size = _previousSceneSize;
            sceneView.Repaint();
        }

        private void ReturnToPreviousScene()
        {
            if (_previewScene.IsValid())
            {
                GameObject[] gos = _previewScene.GetRootGameObjects();
                foreach (GameObject go in gos)
                {
                    GameObject.DestroyImmediate(go);
                }
            }
            if (!string.IsNullOrEmpty(_previousScenePath))
            {
                EditorSceneManager.OpenScene(_previousScenePath);

                if (SceneView.lastActiveSceneView != null)
                    LoadSceneView(SceneView.lastActiveSceneView);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
