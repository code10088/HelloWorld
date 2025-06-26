// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    /// <summary>
    /// Destroys the attached GameObject after specified time
    /// </summary>
    public class GPUIObjectDestroyer : MonoBehaviour
    {
        public float timeToDestroy = 5f;

        private float _enabledTime;

        private void OnEnable()
        {
            _enabledTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _enabledTime > timeToDestroy)
            {
                Destroy(gameObject);
            }
        }
    }
}
