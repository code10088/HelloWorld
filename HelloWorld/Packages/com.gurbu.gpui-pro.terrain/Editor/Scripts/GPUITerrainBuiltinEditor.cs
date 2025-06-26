// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro.TerrainModule
{
    [CustomEditor(typeof(GPUITerrainBuiltin))]
    public class GPUITerrainBuiltinEditor : GPUITerrainEditor
    {
        private GPUITerrainBuiltin _gpuiTerrainBuiltin;

        protected override void OnEnable()
        {
            base.OnEnable();

            _gpuiTerrainBuiltin = target as GPUITerrainBuiltin;
        }

        protected override void DrawTerrainHelpBoxes()
        {
            base.DrawTerrainHelpBoxes();

            if (_gpuiTerrainBuiltin.IsBakedDetailTextures())
                _terrainHelpBoxesVE.Add(GPUIEditorUtility.CreateGPUIHelpBox("bakedDetailTexturesWarning", null, null, HelpBoxMessageType.Warning));
        }

        protected override void DrawActionButtons()
        {
            base.DrawActionButtons();

            if (!_gpuiTerrainBuiltin.IsDetailDensityTexturesLoaded)
                return;

            GPUIEditorTextUtility.GPUIText gpuiText;
            GPUIEditorTextUtility.TryGetGPUIText("bakeDetailTexturesButton", out gpuiText);
            Button button = new Button(() =>
            {
                _gpuiTerrainBuiltin.Editor_EnableBakedDetailTextures();
                _gpuiTerrainBuiltin.Editor_SaveDetailRenderTexturesToBakedTextures();
                EditorUtility.SetDirty(_gpuiTerrainBuiltin);
                RedrawAll();
            })
            {
                text = gpuiText.title,
                tooltip = gpuiText.tooltip
            };
            button.style.backgroundColor = GPUIEditorConstants.Colors.green;
            button.AddToClassList("gpui-action-button");
            button.focusable = false;
            _terrainActionButtonsVE.Add(button);
            GPUIEditorUtility.DrawHelpText(_helpBoxes, gpuiText, _terrainActionButtonsHelpVE);

            if (_gpuiTerrainBuiltin.IsBakedDetailTextures())
            {
                GPUIEditorTextUtility.TryGetGPUIText("deleteBakedDetailTexturesButton", out gpuiText);
                button = new Button(() =>
                {
                    _gpuiTerrainBuiltin.Editor_DeleteBakedDetailTextures();
                    EditorUtility.SetDirty(_gpuiTerrainBuiltin);
                    RedrawAll();
                })
                {
                    text = gpuiText.title,
                    tooltip = gpuiText.tooltip
                };
                button.style.backgroundColor = GPUIEditorConstants.Colors.darkRed;
                button.AddToClassList("gpui-action-button");
                button.focusable = false;
                _terrainActionButtonsVE.Add(button);
            }
        }
    }
}