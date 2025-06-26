// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    [DefaultExecutionOrder(-200)]
    public class GPUIRigidbodySimulator : GPUIColliderHelper<GPUIRigidbodyReplacer>
    {
        private static List<GPUIRigidbodySimulator> _Instances;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_Instances == null)
                _Instances = new List<GPUIRigidbodySimulator>();
            if (!_Instances.Contains(this))
                _Instances.Add(this);
        }

        public static void InitializeInstance(GPUIRigidbodyReplacer instance)
        {
            if (_Instances != null)
            {
                foreach (var simulator in _Instances)
                {
                    if (simulator.IsInsideCollider(instance))
                    {
                        simulator._enteredInstances.Add(instance);
                        simulator.OnEnteredCollider(instance);
                        continue;
                    }
                    if (instance.TryGetComponent(out Rigidbody rigidbody))
                        instance.ReplaceRigidbody(rigidbody);
                }
            }
        }

        protected override void OnEnteredCollider(GPUIRigidbodyReplacer instance)
        {
            instance.AddRigidbody();
            instance.gpuiPrefab.UpdateTransformData();
        }

        protected override bool OnExitedCollider(GPUIRigidbodyReplacer instance)
        {
            if (instance.TryGetComponent(out Rigidbody rigidbody))
            {
                if (!rigidbody.IsSleeping())
                    return false;
                instance.ReplaceRigidbody(rigidbody);
            }
            instance.gpuiPrefab.UpdateTransformData();
            return true;
        }

        protected override void OnUpdate(GPUIRigidbodyReplacer instance)
        {
            instance.gpuiPrefab.UpdateTransformData();
        }
    }
}