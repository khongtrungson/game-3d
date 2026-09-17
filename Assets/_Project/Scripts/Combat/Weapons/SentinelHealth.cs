using System;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Combat
{
    [DisallowMultipleComponent]
    public class SentinelHealth : MonoBehaviour, IDamageable, IFreezableHostile, IResettable
    {
        [Header("Sentinel Health Settings (FR-13)")]
        [Tooltip("Standard Sentinel health (80 HP)")]
        [SerializeField] private int _maxHealth = 80;

        [Header("Event Channels")]
        [SerializeField] private KillEventChannelSO _killChannel;

        private int _currentHealth;
        private bool _isDead;
        private bool _isDisintegrated;
        private bool _isFrozen;
        private float _remainingFreezeTime;

        public event Action OnFrozen;
        public event Action OnUnfrozen;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsDead => _isDead;
        public bool IsDisintegrated => _isDisintegrated;
        public bool IsFrozen => _isFrozen;
        public float RemainingFreezeTime => _remainingFreezeTime;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        private void Update()
        {
            if (_isFrozen && !_isDead)
            {
                _remainingFreezeTime -= Time.deltaTime;
                if (_remainingFreezeTime <= 0f)
                {
                    Unfreeze();
                }
            }
        }

        public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (_isDead) return;

            // FR-21: Logic-Trip Mine triggers freeze hostiles, allowing instant elimination from ANY weapon impact
            if (_isFrozen && amount > 0)
            {
                amount = Mathf.Max(amount, _maxHealth);
            }
            // FR-13: 1-tap headshot kill for Standard Sentinels
            else if (isHeadshot)
            {
                amount = Mathf.Max(amount, _maxHealth);
            }

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            NullLog.Info("Sentinel", $"Sentinel hit for {amount} damage (Headshot: {isHeadshot}, Frozen: {_isFrozen}). Current HP: {_currentHealth}");

            if (_currentHealth <= 0)
            {
                Die(isHeadshot, hitPoint);
            }
        }

        public void ApplyWireframeFreeze(float duration)
        {
            if (_isDead) return;
            _isFrozen = true;
            _remainingFreezeTime = duration;
            NullLog.Info("Sentinel", $"Sentinel frozen into wireframe state for {duration:F1}s!");
            OnFrozen?.Invoke();
        }

        public void Unfreeze()
        {
            if (!_isFrozen) return;
            _isFrozen = false;
            _remainingFreezeTime = 0f;
            NullLog.Info("Sentinel", "Sentinel wireframe freeze expired.");
            OnUnfrozen?.Invoke();
        }

        public void Disintegrate(Vector3 hitPoint)
        {
            if (_isDead) return;
            _isDisintegrated = true;
            _currentHealth = 0;
            Die(true, hitPoint);
        }

        private void Die(bool isHeadshot, Vector3 hitPoint)
        {
            if (_isDead) return;
            _isDead = true;
            _isFrozen = false;

            NullLog.Info("Sentinel", $"Sentinel neutralized! Headshot: {isHeadshot}, Disintegrated: {_isDisintegrated}");
            _killChannel?.RaiseKill(this, isHeadshot, hitPoint);
        }

        public void ResetState()
        {
            _currentHealth = _maxHealth;
            _isDead = false;
            _isDisintegrated = false;
            _isFrozen = false;
            _remainingFreezeTime = 0f;
        }
    }
}
