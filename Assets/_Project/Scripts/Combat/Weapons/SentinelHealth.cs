using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Combat
{
    [DisallowMultipleComponent]
    public class SentinelHealth : MonoBehaviour, IDamageable, IResettable
    {
        [Header("Sentinel Health Settings (FR-13)")]
        [Tooltip("Standard Sentinel health (80 HP)")]
        [SerializeField] private int _maxHealth = 80;

        [Header("Event Channels")]
        [SerializeField] private KillEventChannelSO _killChannel;

        private int _currentHealth;
        private bool _isDead;
        private bool _isDisintegrated;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsDead => _isDead;
        public bool IsDisintegrated => _isDisintegrated;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (_isDead) return;

            // FR-13: 1-tap headshot kill for Standard Sentinels
            if (isHeadshot)
            {
                amount = Mathf.Max(amount, _maxHealth);
            }

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            NullLog.Info("Sentinel", $"Sentinel hit for {amount} damage (Headshot: {isHeadshot}). Current HP: {_currentHealth}");

            if (_currentHealth <= 0)
            {
                Die(isHeadshot, hitPoint);
            }
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

            NullLog.Info("Sentinel", $"Sentinel neutralized! Headshot: {isHeadshot}, Disintegrated: {_isDisintegrated}");
            _killChannel?.RaiseKill(this, isHeadshot, hitPoint);
        }

        public void ResetState()
        {
            _currentHealth = _maxHealth;
            _isDead = false;
            _isDisintegrated = false;
        }
    }
}
