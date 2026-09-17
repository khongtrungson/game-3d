using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Controller
{
    [DisallowMultipleComponent]
    public class FirstPersonCameraLook : MonoBehaviour
    {
        [Header("Target & References")]
        [Tooltip("Root player transform to rotate horizontally (Yaw)")]
        [SerializeField] private Transform _playerBody;

        [Tooltip("Camera pitch pivot (typically this transform or child camera mount)")]
        [SerializeField] private Transform _pitchTransform;

        [Header("Mouse Sensitivity")]
        [SerializeField] private float _mouseSensitivityX = 1.0f;
        [SerializeField] private float _mouseSensitivityY = 1.0f;

        [Header("Pitch Clamping")]
        [SerializeField] private float _minPitchAngle = -85f;
        [SerializeField] private float _maxPitchAngle = 85f;

        [Header("Lean Integration")]
        [Tooltip("Optional reference to CameraLeanController to incorporate roll angle")]
        [SerializeField] private CameraLeanController _leanController;

        private float _currentPitch;

        public float MouseSensitivityX { get => _mouseSensitivityX; set => _mouseSensitivityX = value; }
        public float MouseSensitivityY { get => _mouseSensitivityY; set => _mouseSensitivityY = value; }

        private void Awake()
        {
            if (_pitchTransform == null)
            {
                _pitchTransform = transform;
            }

            if (_leanController == null)
            {
                _leanController = GetComponent<CameraLeanController>();
            }

            Vector3 currentEuler = _pitchTransform.localEulerAngles;
            _currentPitch = currentEuler.x > 180f ? currentEuler.x - 360f : currentEuler.x;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ProcessLook(Vector2 lookDelta)
        {
            // Horizontal rotation on the player body
            float yawDelta = lookDelta.x * _mouseSensitivityX;
            if (_playerBody != null)
            {
                _playerBody.Rotate(Vector3.up * yawDelta);
            }
            else
            {
                transform.Rotate(Vector3.up * yawDelta, Space.World);
            }

            // Vertical rotation (pitch)
            float pitchDelta = lookDelta.y * _mouseSensitivityY;
            _currentPitch = Mathf.Clamp(_currentPitch - pitchDelta, _minPitchAngle, _maxPitchAngle);

            // Apply pitch, taking lean roll into account if present
            float roll = (_leanController != null) ? _leanController.CurrentRollAngle : 0f;
            _pitchTransform.localRotation = Quaternion.Euler(_currentPitch, 0f, roll);
        }

        public void SetPitch(float pitch)
        {
            _currentPitch = Mathf.Clamp(pitch, _minPitchAngle, _maxPitchAngle);
            float roll = (_leanController != null) ? _leanController.CurrentRollAngle : 0f;
            _pitchTransform.localRotation = Quaternion.Euler(_currentPitch, 0f, roll);
        }
    }
}
