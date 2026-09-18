using System;
using UnityEngine;
using UnityEngine.InputSystem;
using NullProtocol.Core;

namespace NullProtocol.Controller
{
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class TacticalLocomotionController : MonoBehaviour, IResettable
    {
        [Header("Locomotion Speeds (FR-1, FR-2, FR-3)")]
        [Tooltip("Base walking speed in m/s (FR-1: 4.0 m/s)")]
        [SerializeField] private float _walkSpeed = 4.0f;

        [Tooltip("Crouch movement speed in m/s (FR-2: 2.2 m/s)")]
        [SerializeField] private float _crouchSpeed = 2.2f;

        [Tooltip("Tactical sprinting speed in m/s (FR-3: 6.2 m/s)")]
        [SerializeField] private float _sprintSpeed = 6.2f;

        [Header("Acceleration & Deceleration (No Floatiness)")]
        [SerializeField] private float _acceleration = 45.0f;
        [SerializeField] private float _deceleration = 60.0f;

        [Header("Crouch Stance (FR-2)")]
        [Tooltip("Eye camera height reduction in meters (FR-2: 0.7m)")]
        [SerializeField] private float _crouchEyeHeightDrop = 0.7f;
        [SerializeField] private float _crouchTransitionDuration = 0.15f;
        [SerializeField] private float _standingEyeHeight = 1.65f;
        [SerializeField] private float _standingColliderHeight = 1.8f;
        [SerializeField] private float _crouchColliderHeight = 1.2f;

        [Header("Tactical Sprint & Sprint-to-Fire Penalty (FR-3)")]
        [Tooltip("Sprint-to-fire transition penalty in seconds (FR-3: 0.25s)")]
        [SerializeField] private float _sprintToFirePenalty = 0.25f;

        [Header("Ground & Gravity Settings (FR-4: No Jumping)")]
        [SerializeField] private float _gravity = 25.0f;
        [SerializeField] private float _groundedStickForce = 4.0f;

        [Header("Camera & Hierarchy References")]
        [Tooltip("Transform holding camera pitch and lean")]
        [SerializeField] private Transform _cameraMount;
        [SerializeField] private CameraLeanController _leanController;
        [SerializeField] private FirstPersonCameraLook _cameraLook;
        [SerializeField] private LocomotionAcousticEmitter _acousticEmitter;

        [Header("Event Channels")]
        [SerializeField] private PlayerStateEventChannelSO _playerStateEvents;

        [Header("Input Action Asset")]
        [SerializeField] private InputActionAsset _inputActions;

        // Runtime State
        private CharacterController _characterController;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private float _leanInput;
        private bool _crouchInputToggle;
        private bool _sprintInputHold;

        private bool _isCrouching;
        private bool _isSprinting;
        private Vector3 _currentHorizontalVelocity;
        private float _verticalVelocity;
        private float _currentEyeHeight;
        private float _eyeHeightVelocity;

        private float _sprintEndTime;
        private bool _canFire;
        private bool _canAds;

        // Input Action references
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _crouchAction;
        private InputAction _sprintAction;
        private InputAction _leanAction;

        // Public Accessors
        public float WalkSpeed => _walkSpeed;
        public float CrouchSpeed => _crouchSpeed;
        public float SprintSpeed => _sprintSpeed;
        public bool IsCrouching => _isCrouching;
        public bool IsSprinting => _isSprinting;
        public bool CanFire => _canFire;
        public bool CanAds => _canAds;
        public float CurrentSpeed => _currentHorizontalVelocity.magnitude;
        public FirstPersonCameraLook CameraLook => _cameraLook;
        public float RecoilModifier => _isCrouching ? 0.7f : 1.0f; // FR-2: 30% recoil reduction when crouched

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_cameraMount != null)
            {
                _standingEyeHeight = _cameraMount.localPosition.y;
            }
            _currentEyeHeight = _standingEyeHeight;

            if (_leanController == null && _cameraMount != null)
            {
                _leanController = _cameraMount.GetComponent<CameraLeanController>();
            }

            if (_cameraLook == null && _cameraMount != null)
            {
                _cameraLook = _cameraMount.GetComponent<FirstPersonCameraLook>();
            }

            if (_acousticEmitter == null)
            {
                _acousticEmitter = GetComponent<LocomotionAcousticEmitter>();
            }

            InitializeInput();
        }

        public InputActionAsset InputActions
        {
            get => _inputActions;
            set
            {
                if (_inputActions != value)
                {
                    DisableInputCallbacks();
                    _inputActions = value;
                    InitializeInput();
                    if (isActiveAndEnabled)
                    {
                        EnableInputCallbacks();
                    }
                }
            }
        }

        private void InitializeInput()
        {
            if (_inputActions == null) return;

            var playerMap = _inputActions.FindActionMap("Player");
            if (playerMap == null) return;

            _moveAction = playerMap.FindAction("Move");
            _lookAction = playerMap.FindAction("Look");
            _crouchAction = playerMap.FindAction("Crouch");
            _sprintAction = playerMap.FindAction("Sprint");
            _leanAction = playerMap.FindAction("Lean");
        }

        private void OnEnable()
        {
            EnableInputCallbacks();
        }

        private void OnDisable()
        {
            DisableInputCallbacks();
        }

        private void EnableInputCallbacks()
        {
            if (_crouchAction != null)
            {
                _crouchAction.performed += OnCrouchPressed;
                _crouchAction.Enable();
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed += OnSprintStarted;
                _sprintAction.canceled += OnSprintCanceled;
                _sprintAction.Enable();
            }

            _moveAction?.Enable();
            _lookAction?.Enable();
            _leanAction?.Enable();
        }

        private void DisableInputCallbacks()
        {
            if (_crouchAction != null)
            {
                _crouchAction.performed -= OnCrouchPressed;
                _crouchAction.Disable();
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed -= OnSprintStarted;
                _sprintAction.canceled -= OnSprintCanceled;
                _sprintAction.Disable();
            }

            _moveAction?.Disable();
            _lookAction?.Disable();
            _leanAction?.Disable();
        }

        private void OnCrouchPressed(InputAction.CallbackContext context)
        {
            // Toggle crouch stance
            SetCrouch(!_isCrouching);
        }

        private void OnSprintStarted(InputAction.CallbackContext context)
        {
            _sprintInputHold = true;
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            _sprintInputHold = false;
        }

        public void SetCrouch(bool crouch)
        {
            if (_isCrouching == crouch) return;

            // If attempting to stand up, verify overhead clearance
            if (!crouch && HasOverheadObstacle())
            {
                NullLog.Info("Locomotion", "Cannot stand up: overhead obstacle detected.");
                return;
            }

            _isCrouching = crouch;

            // Crouching cancels sprinting
            if (_isCrouching && _isSprinting)
            {
                EndSprint();
            }

            _playerStateEvents?.RaiseCrouchChanged(_isCrouching);
            NullLog.Info("Locomotion", $"Crouch stance: {_isCrouching}");
        }

        private bool HasOverheadObstacle()
        {
            Vector3 bottom = transform.position + Vector3.up * _characterController.radius;
            Vector3 topStanding = transform.position + Vector3.up * (_standingColliderHeight - _characterController.radius);
            return Physics.CheckCapsule(bottom, topStanding, _characterController.radius, Layers.Environment, QueryTriggerInteraction.Ignore);
        }

        private void Update()
        {
            PollInput();
            HandleLook();
            HandleLeaning(Time.deltaTime);
            HandleLocomotion(Time.deltaTime);
            HandleCrouchEyeHeight(Time.deltaTime);
            HandleSprintToFirePenalty();
        }

        private void PollInput()
        {
            if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }

            if (_lookAction != null)
            {
                _lookInput = _lookAction.ReadValue<Vector2>();
            }

            if (_leanAction != null)
            {
                _leanInput = _leanAction.ReadValue<float>();
            }
        }

        public void SetManualInputs(Vector2 moveInput, Vector2 lookInput, float leanInput, bool sprintHold)
        {
            _moveInput = moveInput;
            _lookInput = lookInput;
            _leanInput = leanInput;
            _sprintInputHold = sprintHold;
        }

        private void HandleLook()
        {
            if (_cameraLook != null)
            {
                _cameraLook.ProcessLook(_lookInput);
            }
        }

        private void HandleLeaning(float deltaTime)
        {
            if (_leanController != null)
            {
                _leanController.UpdateLeanInput(_leanInput);
                _leanController.TickLean(deltaTime);
            }
        }

        private void HandleLocomotion(float deltaTime)
        {
            // Evaluate sprinting requirements: must have forward input, cannot be crouching, must hold sprint
            bool wantsToSprint = _sprintInputHold && _moveInput.y > 0.1f && !_isCrouching;
            if (wantsToSprint != _isSprinting)
            {
                if (wantsToSprint)
                {
                    StartSprint();
                }
                else
                {
                    EndSprint();
                }
            }

            // Determine target speed based on stance
            float targetSpeed = _walkSpeed;
            if (_isCrouching)
            {
                targetSpeed = _crouchSpeed;
            }
            else if (_isSprinting)
            {
                targetSpeed = _sprintSpeed;
            }

            // Calculate movement direction relative to player orientation (8-directional WASD)
            Vector3 inputDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
            if (inputDirection.sqrMagnitude > 1f)
            {
                inputDirection.Normalize();
            }

            Vector3 worldDirection = transform.TransformDirection(inputDirection);
            Vector3 targetVelocity = worldDirection * targetSpeed;

            // Zero floatiness / sharp acceleration and deceleration
            float accelRate = (targetVelocity.sqrMagnitude > 0.001f) ? _acceleration : _deceleration;
            _currentHorizontalVelocity = Vector3.MoveTowards(_currentHorizontalVelocity, targetVelocity, accelRate * deltaTime);

            // Grounding & Gravity (FR-4: No jumping permitted)
            if (_characterController.isGrounded)
            {
                _verticalVelocity = -_groundedStickForce;
            }
            else
            {
                _verticalVelocity -= _gravity * deltaTime;
            }

            Vector3 totalMovement = (_currentHorizontalVelocity + Vector3.up * _verticalVelocity) * deltaTime;
            _characterController.Move(totalMovement);

            // Process acoustic emissions
            if (_acousticEmitter != null)
            {
                bool isMoving = _currentHorizontalVelocity.sqrMagnitude > 0.05f;
                _acousticEmitter.ProcessMovement(transform.position, _characterController.isGrounded, isMoving, _isCrouching, _isSprinting);
            }
        }

        private void StartSprint()
        {
            _isSprinting = true;
            _canAds = false;   // FR-3: Disable ADS while sprinting
            _canFire = false;  // FR-3: Disable hip-fire while sprinting
            _playerStateEvents?.RaiseSprintChanged(true);
        }

        private void EndSprint()
        {
            if (!_isSprinting) return;

            _isSprinting = false;
            _sprintEndTime = Time.time;
            _playerStateEvents?.RaiseSprintChanged(false);
        }

        private void HandleSprintToFirePenalty()
        {
            if (_isSprinting)
            {
                _canFire = false;
                _canAds = false;
                return;
            }

            // Enforce 0.25s sprint-to-fire transition penalty
            float timeSinceSprint = Time.time - _sprintEndTime;
            bool ready = timeSinceSprint >= _sprintToFirePenalty;
            _canFire = ready;
            _canAds = ready;
        }

        private void HandleCrouchEyeHeight(float deltaTime)
        {
            float targetHeight = _isCrouching ? (_standingEyeHeight - _crouchEyeHeightDrop) : _standingEyeHeight;
            _currentEyeHeight = Mathf.SmoothDamp(_currentEyeHeight, targetHeight, ref _eyeHeightVelocity, _crouchTransitionDuration, float.PositiveInfinity, deltaTime);

            if (_cameraMount != null)
            {
                Vector3 camPos = _cameraMount.localPosition;
                camPos.y = _currentEyeHeight;
                _cameraMount.localPosition = camPos;

                if (_leanController != null)
                {
                    _leanController.SetNeutralPosition(camPos);
                }
            }

            // Adjust character controller height
            float targetColliderHeight = _isCrouching ? _crouchColliderHeight : _standingColliderHeight;
            _characterController.height = Mathf.MoveTowards(_characterController.height, targetColliderHeight, (_standingColliderHeight - _crouchColliderHeight) / _crouchTransitionDuration * deltaTime);
            _characterController.center = Vector3.up * (_characterController.height * 0.5f);
        }

        public void ResetState()
        {
            _isCrouching = false;
            _isSprinting = false;
            _sprintInputHold = false;
            _currentHorizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _canFire = true;
            _canAds = true;
            _currentEyeHeight = _standingEyeHeight;

            if (_cameraMount != null)
            {
                Vector3 camPos = _cameraMount.localPosition;
                camPos.y = _standingEyeHeight;
                _cameraMount.localPosition = camPos;
            }

            _characterController.height = _standingColliderHeight;
            _characterController.center = Vector3.up * (_standingColliderHeight * 0.5f);

            _playerStateEvents?.RaiseCrouchChanged(false);
            _playerStateEvents?.RaiseSprintChanged(false);
            _playerStateEvents?.RaiseLeanChanged(0f);
        }
    }
}
