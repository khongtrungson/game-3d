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

        [Header("Mouse Sensitivity & Processing (FR-45)")]
        [Tooltip("Overall mouse sensitivity slider (0.1 to 10.0)")]
        [Range(0.1f, 10.0f)]
        [SerializeField] private float _mouseSensitivity = 1.0f;
        [SerializeField] private float _mouseSensitivityX = 1.0f;
        [SerializeField] private float _mouseSensitivityY = 1.0f;
        [Tooltip("FR-45: Raw mouse input processing (unfiltered sub-frame input)")]
        [SerializeField] private bool _rawMouseInput = true;
        [Tooltip("FR-45: Mouse acceleration toggle (default OFF)")]
        [SerializeField] private bool _mouseAcceleration = false;
        [Tooltip("FR-45: Y-axis invert toggle")]
        [SerializeField] private bool _invertY = false;
        [SerializeField] private float _accelerationFactor = 1.5f;

        [Header("Pitch Clamping")]
        [SerializeField] private float _minPitchAngle = -85f;
        [SerializeField] private float _maxPitchAngle = 85f;

        [Header("Lean Integration")]
        [Tooltip("Optional reference to CameraLeanController to incorporate roll angle")]
        [SerializeField] private CameraLeanController _leanController;

        private float _currentPitch;

        public float MouseSensitivity
        {
            get => _mouseSensitivity;
            set => _mouseSensitivity = Mathf.Clamp(value, 0.1f, 10.0f);
        }
        public float MouseSensitivityX { get => _mouseSensitivityX; set => _mouseSensitivityX = value; }
        public float MouseSensitivityY { get => _mouseSensitivityY; set => _mouseSensitivityY = value; }
        public bool RawMouseInput { get => _rawMouseInput; set => _rawMouseInput = value; }
        public bool MouseAcceleration { get => _mouseAcceleration; set => _mouseAcceleration = value; }
        public bool InvertY { get => _invertY; set => _invertY = value; }

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

        public void ApplySettings(GameSettingsData settings)
        {
            if (settings == null) return;
            _mouseSensitivity = Mathf.Clamp(settings.MouseSensitivity, 0.1f, 10.0f);
            _rawMouseInput = settings.RawMouseInput;
            _mouseAcceleration = settings.MouseAcceleration;
            _invertY = settings.InvertY;
        }

        public void ProcessLook(Vector2 lookDelta)
        {
            Vector2 processedDelta = lookDelta;

            // Apply mouse acceleration if enabled (default OFF per FR-45)
            if (_mouseAcceleration)
            {
                float speed = lookDelta.magnitude;
                float accel = 1.0f + (speed * 0.05f * _accelerationFactor);
                processedDelta *= accel;
            }

            // Horizontal rotation on the player body (Yaw)
            float yawDelta = processedDelta.x * _mouseSensitivity * _mouseSensitivityX;
            if (_playerBody != null)
            {
                _playerBody.Rotate(Vector3.up * yawDelta);
            }
            else
            {
                transform.Rotate(Vector3.up * yawDelta, Space.World);
            }

            // Vertical rotation (Pitch) with Y-axis invert support (FR-45)
            float pitchSign = _invertY ? 1.0f : -1.0f;
            float pitchDelta = processedDelta.y * _mouseSensitivity * _mouseSensitivityY * pitchSign;
            _currentPitch = Mathf.Clamp(_currentPitch + pitchDelta, _minPitchAngle, _maxPitchAngle);

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
