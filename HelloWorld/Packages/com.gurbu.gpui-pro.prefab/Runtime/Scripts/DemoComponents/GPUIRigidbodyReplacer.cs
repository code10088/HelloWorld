// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(GPUIPrefab))]
    public class GPUIRigidbodyReplacer : GPUIPrefabExtension
    {
        [SerializeField]
        public GPUIRigidbodyData rigidbodyData;

        protected override void Start()
        {
            base.Start();
            GPUIRigidbodySimulator.InitializeInstance(this);
        }

        internal void ReplaceRigidbody(Rigidbody rigidbody)
        {
            if (rigidbodyData == null)
                rigidbodyData = new GPUIRigidbodyData(rigidbody);
            Destroy(rigidbody);
        }

        internal void AddRigidbody()
        {
            if (rigidbodyData != null && !gameObject.HasComponent<Rigidbody>())
                rigidbodyData.SetValuesToRigidbody(gameObject.AddComponent<Rigidbody>());
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            if (TryGetComponent(out Rigidbody rigidbody))
                rigidbodyData = new GPUIRigidbodyData(rigidbody);
        }
#endif

        [Serializable]
        public class GPUIRigidbodyData
        {
            public bool useGravity;
            public float angularDrag;
            public float mass;
            public RigidbodyConstraints constraints;
            public float drag;
            public bool isKinematic;
            public RigidbodyInterpolation interpolation;

            public GPUIRigidbodyData(Rigidbody rigidbody)
            {
                useGravity = rigidbody.useGravity;
#if UNITY_6000_0_OR_NEWER
                angularDrag = rigidbody.angularDamping;
#else
                angularDrag = rigidbody.angularDrag;
#endif
                mass = rigidbody.mass;
                constraints = rigidbody.constraints;
#if UNITY_6000_0_OR_NEWER
                drag = rigidbody.linearDamping;
#else
                drag = rigidbody.drag;
#endif
                isKinematic = rigidbody.isKinematic;
                interpolation = rigidbody.interpolation;
            }

            public void SetValuesToRigidbody(Rigidbody rigidbody)
            {
                rigidbody.useGravity = useGravity;
#if UNITY_6000_0_OR_NEWER
                rigidbody.angularDamping = angularDrag;
#else
                rigidbody.angularDrag = angularDrag;
#endif
                rigidbody.mass = mass;
                rigidbody.constraints = constraints;
                rigidbody.detectCollisions = true;
#if UNITY_6000_0_OR_NEWER
                rigidbody.linearDamping = drag;
#else
                rigidbody.drag = drag;
#endif
                rigidbody.isKinematic = isKinematic;
                rigidbody.interpolation = interpolation;
            }
        }
    }
}