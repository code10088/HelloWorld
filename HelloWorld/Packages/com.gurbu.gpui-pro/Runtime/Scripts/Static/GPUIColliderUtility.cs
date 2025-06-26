// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro
{
    public static class GPUIColliderUtility
    {
        /// <summary>
        /// Method to copy collider values from one GameObject to another
        /// </summary>
        public static Collider CopyColliderValues(Collider source, Vector3 centerOffset, GameObject target)
        {
            if (source == null || target == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Source or Target is null.");
                return null;
            }

            Collider targetCollider = null;

            // Check the type of the collider and create the corresponding collider on the target object
            if (source is BoxCollider sourceBox)
            {
                BoxCollider targetBox = target.AddComponent<BoxCollider>();
                CopyBoxColliderValues(sourceBox, targetBox, centerOffset);
                targetCollider = targetBox;
            }
            else if (source is SphereCollider sourceSphere)
            {
                SphereCollider targetSphere = target.AddComponent<SphereCollider>();
                CopySphereColliderValues(sourceSphere, targetSphere, centerOffset);
                targetCollider = targetSphere;
            }
            else if (source is CapsuleCollider sourceCapsule)
            {
                CapsuleCollider targetCapsule = target.AddComponent<CapsuleCollider>();
                CopyCapsuleColliderValues(sourceCapsule, targetCapsule, centerOffset);
                targetCollider = targetCapsule;
            }
            else if (source is MeshCollider sourceMesh)
            {
                MeshCollider targetMesh = target.AddComponent<MeshCollider>();
                CopyMeshColliderValues(sourceMesh, targetMesh, centerOffset);
                targetCollider = targetMesh;
            }

            return targetCollider;
        }

        /// <summary>
        /// Copy BoxCollider specific values
        /// </summary>
        public static void CopyBoxColliderValues(BoxCollider source, BoxCollider target, Vector3 offset)
        {
            target.center = source.center + offset;
            target.size = source.size;
            CopyColliderValues(source, target);
        }

        /// <summary>
        /// Copy SphereCollider specific values
        /// </summary>
        public static void CopySphereColliderValues(SphereCollider source, SphereCollider target, Vector3 offset)
        {
            target.center = source.center + offset;
            target.radius = source.radius;
            CopyColliderValues(source, target);
        }

        /// <summary>
        /// Copy CapsuleCollider specific values
        /// </summary>
        public static void CopyCapsuleColliderValues(CapsuleCollider source, CapsuleCollider target, Vector3 offset)
        {
            target.center = source.center + offset;
            target.radius = source.radius;
            target.height = source.height;
            target.direction = source.direction;
            CopyColliderValues(source, target);
        }

        /// <summary>
        /// Copy MeshCollider specific values
        /// </summary>
        public static void CopyMeshColliderValues(MeshCollider source, MeshCollider target, Vector3 offset)
        {
            target.sharedMesh = source.sharedMesh;
            target.convex = source.convex;
            CopyColliderValues(source, target);
        }

        /// <summary>
        /// Copy base Collider values
        /// </summary>
        private static void CopyColliderValues(Collider source, Collider target)
        {
            target.isTrigger = source.isTrigger;
            target.contactOffset = source.contactOffset;
            target.sharedMaterial = source.sharedMaterial;
            target.excludeLayers = source.excludeLayers;
        }

        /// <summary>
        /// Disables all colliders on a GameObject that are not Mesh Colliders and adds Mesh Colliders for each Mesh Filter.
        /// </summary>
        public static void ReplaceOtherCollidersWithMeshColliders(GameObject parentGO, out List<Collider> disabledColliders, out List<MeshCollider> addedColliders, int layerMask)
        {
            disabledColliders = new(parentGO.GetComponentsInChildren<Collider>());
            for (int i = 0; i < disabledColliders.Count; i++)
            {
                Collider collider = disabledColliders[i];
                if (collider is not MeshCollider && collider.enabled && GPUIUtility.IsInLayer(layerMask, collider.gameObject.layer))
                {
                    collider.enabled = false;
                    continue;
                }

                disabledColliders.RemoveAt(i);
                i--;
            }

            addedColliders = new List<MeshCollider>();
            AddMeshCollidersForEachMeshFilter(parentGO.transform, ref addedColliders, layerMask);
        }

        /// <summary>
        /// Adds Mesh Collider for each Mesh Filter on the given Transform and its children.
        /// </summary>
        private static void AddMeshCollidersForEachMeshFilter(Transform parentTransform, ref List<MeshCollider> addedColliders, int layerMask)
        {
            if (!parentTransform.HasComponent<MeshCollider>() && GPUIUtility.IsInLayer(layerMask, parentTransform.gameObject.layer) && parentTransform.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
            {
                MeshCollider collider = parentTransform.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = meshFilter.sharedMesh;
                addedColliders.Add(collider);
            }
            for (int i = 0; i < parentTransform.childCount; i++)
                AddMeshCollidersForEachMeshFilter(parentTransform.GetChild(i), ref addedColliders, layerMask);
        }

        /// <summary>
        /// Reverts changes made with the <see cref="ReplaceOtherCollidersWithMeshColliders">ReplaceOtherCollidersWithMeshColliders</see> method.
        /// </summary>
        /// <param name="disabledColliders"></param>
        /// <param name="addedColliders"></param>
        public static void RevertAddedMeshCollidersAndDisabledColliders(List<Collider> disabledColliders, List<MeshCollider> addedColliders)
        {
            foreach (var addedCollider in addedColliders)
            {
                addedCollider.DestroyGeneric();
            }
            foreach (var disabledCollider in disabledColliders)
            {
                disabledCollider.enabled = true;
            }
        }

        public static void CopyRigidbodySettings(Rigidbody source, Rigidbody target)
        {
#if UNITY_6000_0_OR_NEWER
            target.linearDamping = source.linearDamping;
            target.angularDamping = source.angularDamping;
#else
            target.drag = source.drag;
            target.angularDrag = source.angularDrag;
#endif
            target.interpolation = source.interpolation;
            target.collisionDetectionMode = source.collisionDetectionMode;
            target.useGravity = source.useGravity;
            target.isKinematic = source.isKinematic;
            target.constraints = source.constraints;
            target.maxAngularVelocity = source.maxAngularVelocity;
            target.sleepThreshold = source.sleepThreshold;


        }
    }
}