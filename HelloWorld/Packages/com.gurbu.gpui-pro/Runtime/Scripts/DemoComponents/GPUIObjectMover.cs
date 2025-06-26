// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIObjectMover : MonoBehaviour
    {
        [Range(-100f, 100f)]
        public float forwardMove;
        [Range(-100f, 100f)]
        public float upwardMove;
        public Vector3 positionChange;
        public Vector3 rotationChange;
        public bool isLooping;
        public float loopDistance;
        public float loopAngle;
        public bool loopChangeDirection;

        public bool isRayCasting;
        public float rayCastHeight = 15f;
        public int rayCastLayer = 6;
        public float rayCastMaxDistance = 200f;

        public bool isOrbiting;
        public Transform orbitCenter;
        public float orbitSpeed = 1f;

        private Transform _cachedTransform;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private float _orbitDistance;
        private bool _loopChangedDirection;

        private void OnEnable()
        {
            _cachedTransform = transform;
            _startPosition = _cachedTransform.position;
            _startRotation = _cachedTransform.rotation;
            if (orbitCenter != null)
                _orbitDistance = Vector3.Distance(_startPosition, orbitCenter.position);
        }

        private void Update()
        {
            Vector3 currentPos = _cachedTransform.position;
            Quaternion currentRotation;
            if (isOrbiting)
            {
                if (orbitCenter == null)
                    return;
                Vector3 orbitCenterPos = orbitCenter.position;
                Vector3 targetPos = currentPos + _cachedTransform.right * orbitSpeed * Time.deltaTime;
                targetPos.y = _startPosition.y;
                _cachedTransform.position = targetPos;
                _cachedTransform.LookAt(orbitCenter);

                targetPos = orbitCenterPos - _cachedTransform.forward * _orbitDistance;
                targetPos.y = _startPosition.y;
                _cachedTransform.position = targetPos;

                currentRotation = _cachedTransform.rotation;
                Vector3 newEulerAngles = currentRotation.eulerAngles;
                newEulerAngles.x = _startRotation.eulerAngles.x;
                _cachedTransform.rotation = Quaternion.Euler(newEulerAngles);
                return;
            }
            currentRotation = _cachedTransform.rotation;
            if (isLooping)
            {
                if (_loopChangedDirection && (
                    (loopDistance > 0 && 0.1f > Vector3.Distance(currentPos, _startPosition)) ||
                    (loopAngle > 0 && 0.5f > Mathf.Abs(Quaternion.Angle(currentRotation, _startRotation)))
                    ))
                {
                    _loopChangedDirection = false;
                    forwardMove = -forwardMove;
                    rotationChange = -rotationChange;
                }
                else if ((loopDistance > 0 && loopDistance < Vector3.Distance(currentPos, _startPosition)) ||
                (loopAngle > 0 && loopAngle < Mathf.Abs(Quaternion.Angle(currentRotation, _startRotation))))
                {
                    if (loopChangeDirection)
                    {
                        _loopChangedDirection = true;
                        forwardMove = -forwardMove;
                        rotationChange = -rotationChange;
                    }
                    else
                    {
                        _cachedTransform.position = _startPosition;
                        _cachedTransform.rotation = _startRotation;
                        return;
                    }
                }
            }
            Vector3 newPos = currentPos;
            if (forwardMove != 0)
                newPos += _cachedTransform.forward * forwardMove * Time.deltaTime;

            if (upwardMove != 0)
                newPos += _cachedTransform.up * upwardMove * Time.deltaTime;
            
            newPos += positionChange * Time.deltaTime;

            if (isRayCasting)
            {
                if (Physics.Raycast(newPos, Vector3.down, out RaycastHit hit, rayCastMaxDistance, 1 << rayCastLayer))
                    newPos.y = hit.point.y + rayCastHeight;
            }
            _cachedTransform.position = newPos;

            Vector3 eulerAngles = currentRotation.eulerAngles + rotationChange * Time.deltaTime;
            _cachedTransform.rotation = Quaternion.Euler(eulerAngles);
        }

        public void ResetToStartingPositionAndRotation()
        {
            _cachedTransform.position = _startPosition;
            _cachedTransform.rotation = _startRotation;
        }
    }
}
