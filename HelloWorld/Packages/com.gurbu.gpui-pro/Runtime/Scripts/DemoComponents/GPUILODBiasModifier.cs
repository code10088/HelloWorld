// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUILODBiasModifier : MonoBehaviour
    {
        public float lodBias = 2f;

        private float _previousLODBias;

        private void OnEnable()
        {
            _previousLODBias = QualitySettings.lodBias;
            QualitySettings.lodBias = lodBias;
        }

        private void OnDisable()
        {
            QualitySettings.lodBias = _previousLODBias;
        }
    }
}
