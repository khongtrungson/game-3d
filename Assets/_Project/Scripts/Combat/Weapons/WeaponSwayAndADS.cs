using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Combat
{
    public class WeaponSwayAndADS : MonoBehaviour
    {
        [Header("Camera & ADS Settings (FR-14)")]
        [SerializeField] private Camera _playerCamera;
        [Tooltip("Narrow FOV by 15% during ADS (0.85 multiplier)")]
        [SerializeField] private float _adsFovMultiplier = 0.85f;
        [SerializeField] private float _adsTransitionSpeed = 10.0f;

        [Header("Weapon Sway Settings")]
        [Tooltip("Positional sway amount based on mouse look delta")]
        [SerializeField] private float _swayAmount = 0.02f;
        [SerializeField] private float _maxSwayOffset = 0.05f;
        [SerializeField] private float _swaySmoothness = 6.0f;

        [Tooltip("Dampen procedural weapon sway by 80% (0.2x multiplier) during ADS (FR-14)")]
        [SerializeField] private float _adsSwayMultiplier = 0.2f;

        [Header("Optical Reticle & Sights Alignment")]
        [SerializeField] private Transform _weaponRootTransform;
        [SerializeField] private Vector3 _hipfireLocalPosition = new Vector3(0.2f, -0.2f, 0.4f);
        [SerializeField] private Vector3 _adsLocalPosition = new Vector3(0f, -0.12f, 0.35f);
        [SerializeField] private GameObject _opticalReticleObject;

        // Runtime State
        private bool _isAiming;
        private float _baseFov = 75.0f;
        private float _currentFov;
        private Vector3 _currentSwayOffset;
        private Vector3 _swayVelocity;

        public bool IsAiming => _isAiming;
        public float CurrentFov => _currentFov;
        public float BaseFov
        {
            get => _baseFov;
            set => SetBaseFov(value);
        }
        public float AdsSwayMultiplier => _adsSwayMultiplier;
        public float AdsFovMultiplier => _adsFovMultiplier;

        public void SetBaseFov(float fov)
        {
            _baseFov = Mathf.Clamp(fov, 80.0f, 110.0f);
            if (!_isAiming)
            {
                _currentFov = _baseFov;
                if (_playerCamera != null)
                {
                    _playerCamera.fieldOfView = _baseFov;
                }
            }
        }

        private void Awake()
        {
            if (_playerCamera != null)
            {
                _baseFov = _playerCamera.fieldOfView;
            }
            _currentFov = _baseFov;

            if (_opticalReticleObject != null)
            {
                _opticalReticleObject.SetActive(false);
            }
        }

        public void SetAimDownSights(bool isAiming)
        {
            _isAiming = isAiming;
            if (_opticalReticleObject != null)
            {
                _opticalReticleObject.SetActive(isAiming);
            }
        }

        public void TickADSAndSway(float deltaTime, Vector2 lookInput)
        {
            // 1. Field of view zoom (15% reduction -> 85% of base)
            float targetFov = _isAiming ? (_baseFov * _adsFovMultiplier) : _baseFov;
            _currentFov = Mathf.Lerp(_currentFov, targetFov, _adsTransitionSpeed * deltaTime);
            if (_playerCamera != null)
            {
                _playerCamera.fieldOfView = _currentFov;
            }

            // 2. Procedural weapon sway calculation with 80% dampening on ADS
            float swayDamping = _isAiming ? _adsSwayMultiplier : 1.0f;
            float targetSwayX = Mathf.Clamp(-lookInput.x * _swayAmount * swayDamping, -_maxSwayOffset, _maxSwayOffset);
            float targetSwayY = Mathf.Clamp(-lookInput.y * _swayAmount * swayDamping, -_maxSwayOffset, _maxSwayOffset);
            Vector3 targetSway = new Vector3(targetSwayX, targetSwayY, 0f);

            _currentSwayOffset = Vector3.Lerp(_currentSwayOffset, targetSway, _swaySmoothness * deltaTime);

            // 3. Align optical reticle & weapon position
            if (_weaponRootTransform != null)
            {
                Vector3 targetPos = _isAiming ? _adsLocalPosition : _hipfireLocalPosition;
                _weaponRootTransform.localPosition = Vector3.Lerp(_weaponRootTransform.localPosition, targetPos + _currentSwayOffset, _adsTransitionSpeed * deltaTime);
            }
        }

        public void ResetADS()
        {
            _isAiming = false;
            _currentFov = _baseFov;
            if (_playerCamera != null)
            {
                _playerCamera.fieldOfView = _baseFov;
            }
            if (_opticalReticleObject != null)
            {
                _opticalReticleObject.SetActive(false);
            }
            _currentSwayOffset = Vector3.zero;
        }
    }
}
