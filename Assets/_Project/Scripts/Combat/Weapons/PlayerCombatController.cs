using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NullProtocol.Core;

namespace NullProtocol.Combat
{
    [DisallowMultipleComponent]
    public class PlayerCombatController : MonoBehaviour, IResettable
    {
        [Header("Weapon Inventory & Loadout")]
        [SerializeField] private List<BaseWeapon> _weapons = new List<BaseWeapon>();
        [SerializeField] private int _currentWeaponIndex = 0;

        [Header("Raycast & Firing Origins")]
        [SerializeField] private Transform _cameraMount;
        [SerializeField] private Camera _playerCamera;

        [Header("Components")]
        [SerializeField] private WeaponSwayAndADS _swayAndAds;
        [SerializeField] private MunitionsEconomyManager _economyManager;

        [Header("Input Action Asset")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Locomotion Constraints")]
        [Tooltip("When true, firing and ADS are blocked (e.g. during sprint)")]
        [SerializeField] private bool _sprintRestricted = false;

        [Tooltip("Recoil modifier based on posture (e.g. 0.7 when crouched)")]
        [SerializeField] private float _postureRecoilModifier = 1.0f;

        // Input Actions
        private InputAction _attackAction;
        private InputAction _adsAction;
        private InputAction _reloadAction;
        private InputAction _switchModeAction;
        private InputAction _previousWeaponAction;
        private InputAction _nextWeaponAction;
        private InputAction _lookAction;

        private Vector2 _lookInput;
        private bool _isFiringInput;
        private bool _isAdsInput;

        public BaseWeapon CurrentWeapon => (_weapons.Count > 0 && _currentWeaponIndex >= 0 && _currentWeaponIndex < _weapons.Count) 
            ? _weapons[_currentWeaponIndex] 
            : null;

        public int CurrentWeaponIndex => _currentWeaponIndex;
        public WeaponSwayAndADS SwayAndAds => _swayAndAds;
        public MunitionsEconomyManager EconomyManager => _economyManager;

        public bool SprintRestricted { get => _sprintRestricted; set => _sprintRestricted = value; }
        public float PostureRecoilModifier { get => _postureRecoilModifier; set => _postureRecoilModifier = value; }
        public bool IsAdsInput => _isAdsInput;

        private void Awake()
        {
            if (_playerCamera == null && _cameraMount != null)
            {
                _playerCamera = _cameraMount.GetComponentInChildren<Camera>();
            }

            if (_swayAndAds == null)
            {
                _swayAndAds = GetComponentInChildren<WeaponSwayAndADS>();
            }

            if (_economyManager == null)
            {
                _economyManager = GetComponent<MunitionsEconomyManager>() ?? gameObject.AddComponent<MunitionsEconomyManager>();
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

            _attackAction = playerMap.FindAction("Attack");
            _lookAction = playerMap.FindAction("Look");
            _reloadAction = playerMap.FindAction("Reload");
            _previousWeaponAction = playerMap.FindAction("Previous");
            _nextWeaponAction = playerMap.FindAction("Next");
            _adsAction = playerMap.FindAction("Aim") ?? playerMap.FindAction("Interact"); // fallback or custom binding
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
            if (_attackAction != null)
            {
                _attackAction.performed += OnAttackPerformed;
                _attackAction.canceled += OnAttackCanceled;
                _attackAction.Enable();
            }

            if (_reloadAction != null)
            {
                _reloadAction.performed += OnReloadPerformed;
                _reloadAction.Enable();
            }

            if (_adsAction != null)
            {
                _adsAction.performed += OnAdsPerformed;
                _adsAction.canceled += OnAdsCanceled;
                _adsAction.Enable();
            }

            if (_previousWeaponAction != null)
            {
                _previousWeaponAction.performed += OnPreviousWeaponPerformed;
                _previousWeaponAction.Enable();
            }

            if (_nextWeaponAction != null)
            {
                _nextWeaponAction.performed += OnNextWeaponPerformed;
                _nextWeaponAction.Enable();
            }

            _lookAction?.Enable();
        }

        private void DisableInputCallbacks()
        {
            if (_attackAction != null)
            {
                _attackAction.performed -= OnAttackPerformed;
                _attackAction.canceled -= OnAttackCanceled;
                _attackAction.Disable();
            }

            if (_reloadAction != null)
            {
                _reloadAction.performed -= OnReloadPerformed;
                _reloadAction.Disable();
            }

            if (_adsAction != null)
            {
                _adsAction.performed -= OnAdsPerformed;
                _adsAction.canceled -= OnAdsCanceled;
                _adsAction.Disable();
            }

            if (_previousWeaponAction != null)
            {
                _previousWeaponAction.performed -= OnPreviousWeaponPerformed;
                _previousWeaponAction.Disable();
            }

            if (_nextWeaponAction != null)
            {
                _nextWeaponAction.performed -= OnNextWeaponPerformed;
                _nextWeaponAction.Disable();
            }

            _lookAction?.Disable();
        }

        private void OnPreviousWeaponPerformed(InputAction.CallbackContext ctx)
        {
            SwitchWeaponRelative(-1);
        }

        private void OnNextWeaponPerformed(InputAction.CallbackContext ctx)
        {
            SwitchWeaponRelative(1);
        }

        private void Update()
        {
            if (_lookAction != null)
            {
                _lookInput = _lookAction.ReadValue<Vector2>();
            }

            // Update procedural sway & ADS FOV
            if (_swayAndAds != null)
            {
                _swayAndAds.TickADSAndSway(Time.deltaTime, _lookInput);
            }

            // Update recoil spring on current weapon
            if (CurrentWeapon != null && CurrentWeapon.RecoilSpring != null)
            {
                CurrentWeapon.RecoilSpring.TickSpring(Time.deltaTime);
            }

            // Process firing if holding attack (for semi/burst/charge)
            if (_isFiringInput && !_sprintRestricted)
            {
                TryFireCurrentWeapon();
            }
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            _isFiringInput = true;
            if (!_sprintRestricted)
            {
                TryFireCurrentWeapon();
            }
        }

        private void OnAttackCanceled(InputAction.CallbackContext context)
        {
            _isFiringInput = false;
            // If weapon is PhaseRail, canceling button might cancel charge
            if (CurrentWeapon is PhaseRail phaseRail)
            {
                // Charge can be held or auto-fired depending on design; PhaseRail charges on trigger
            }
        }

        private void OnAdsPerformed(InputAction.CallbackContext context)
        {
            if (_sprintRestricted) return;
            _isAdsInput = true;
            _swayAndAds?.SetAimDownSights(true);
        }

        private void OnAdsCanceled(InputAction.CallbackContext context)
        {
            _isAdsInput = false;
            _swayAndAds?.SetAimDownSights(false);
        }

        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            CurrentWeapon?.Reload();
        }

        public void TryFireCurrentWeapon()
        {
            if (CurrentWeapon == null || _sprintRestricted) return;

            Vector3 shootOrigin = (_cameraMount != null) ? _cameraMount.position : transform.position;
            Vector3 shootDirection = (_cameraMount != null) ? _cameraMount.forward : transform.forward;

            // Incorporate weapon recoil spring rotation offset into shoot direction
            if (CurrentWeapon.RecoilSpring != null)
            {
                Quaternion recoilRot = Quaternion.Euler(CurrentWeapon.RecoilSpring.CurrentRotation);
                shootDirection = recoilRot * shootDirection;
            }

            CurrentWeapon.PrimaryFire(shootOrigin, shootDirection, _postureRecoilModifier);
        }

        public void SwitchWeapon(int index)
        {
            if (index < 0 || index >= _weapons.Count || index == _currentWeaponIndex) return;

            if (CurrentWeapon != null)
            {
                CurrentWeapon.gameObject.SetActive(false);
            }

            _currentWeaponIndex = index;
            CurrentWeapon.gameObject.SetActive(true);
            NullLog.Info("Combat", $"Equipped weapon: {CurrentWeapon.Data?.WeaponName}");
        }

        public void SwitchWeaponRelative(int offset)
        {
            if (_weapons.Count <= 1) return;
            int nextIndex = (_currentWeaponIndex + offset) % _weapons.Count;
            if (nextIndex < 0) nextIndex += _weapons.Count;
            SwitchWeapon(nextIndex);
        }

        public void SetWeapons(List<BaseWeapon> weapons)
        {
            _weapons = weapons;
            for (int i = 0; i < _weapons.Count; i++)
            {
                _weapons[i].gameObject.SetActive(i == _currentWeaponIndex);
            }
        }

        public void SetSprintRestriction(bool isSprinting)
        {
            _sprintRestricted = isSprinting;
            if (_sprintRestricted)
            {
                _isAdsInput = false;
                _swayAndAds?.SetAimDownSights(false);
            }
        }

        public void ResetState()
        {
            _isFiringInput = false;
            _isAdsInput = false;
            _sprintRestricted = false;
            _swayAndAds?.ResetADS();
            _economyManager?.ResetState();

            foreach (var w in _weapons)
            {
                w?.ResetState();
            }
        }
    }
}
