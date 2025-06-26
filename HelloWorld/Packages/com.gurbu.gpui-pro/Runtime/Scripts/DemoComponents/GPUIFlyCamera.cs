// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEngine;

namespace GPUInstancerPro
{
    public class GPUIFlyCamera : GPUIInputHandler
    {
        public float mainSpeed = 10.0f;
        public float shiftSpeed = 30.0f;
        public float rotationSpeed = 5.0f;

        private Vector3 _inputVector;
        private Vector3 _rotationEuler;

        protected override void Start()
        {
            base.Start();
            _inputVector = Vector3.zero;
            _rotationEuler = transform.rotation.eulerAngles;
        }

        void Update()
        {
            if (GetMouseButton(1))
            {
                float yAxis = GetAxis("Mouse Y");
                float xAxis = GetAxis("Mouse X");
                if (Mathf.Abs(yAxis) < 5f && Mathf.Abs(xAxis) < 5f)
                {
                    _rotationEuler.x -= yAxis * rotationSpeed;
                    _rotationEuler.y += xAxis * rotationSpeed;
                    transform.eulerAngles = _rotationEuler;
                }
            }

            CalculateInputVector();

            transform.Translate(_inputVector);
        }

        private void CalculateInputVector()
        {
            _inputVector.x = 0;
            _inputVector.y = 0;
            _inputVector.z = 0;

            if (GetKey(KeyCode.W))
                _inputVector.z += 1;
            if (GetKey(KeyCode.S))
                _inputVector.z -= 1;
            if (GetKey(KeyCode.A))
                _inputVector.x -= 1;
            if (GetKey(KeyCode.D))
                _inputVector.x += 1;
            if (GetKey(KeyCode.Q))
                _inputVector.y -= 1;
            if (GetKey(KeyCode.E))
                _inputVector.y += 1;
            if (GetKey(KeyCode.LeftShift))
                _inputVector *= Time.deltaTime * shiftSpeed;
            else
                _inputVector *= Time.deltaTime * mainSpeed;
        }
    }
}