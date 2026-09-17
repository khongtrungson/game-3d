using System.Collections;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Controller
{
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour, IDamageable, IResettable
    {
        [Header("Health Settings")]
        [SerializeField] private int _maxHealth = 100;

        [Header("Event Channels")]
        [SerializeField] private PlayerStateEventChannelSO _playerStateEvents;
        [SerializeField] private VoidEventChannelSO _memoryDumpResetEvents;

        [Header("Death Reset Settings")]
        [SerializeField] private float _memoryDumpDuration = 2.0f;

        private int _currentHealth;
        private bool _isDead;
        private Coroutine _deathRoutine;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsDead => _isDead;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        private void Start()
        {
            _playerStateEvents?.RaiseHealthChanged(_currentHealth, _maxHealth);
        }

        public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (_isDead) return;

            int effectiveDamage = Mathf.Max(0, amount);
            _currentHealth = Mathf.Max(0, _currentHealth - effectiveDamage);

            NullLog.Info("Health", $"Player took {effectiveDamage} damage. Current HP: {_currentHealth}");
            _playerStateEvents?.RaiseHealthChanged(_currentHealth, _maxHealth);

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            NullLog.Info("Health", "Player health reached 0. Triggering Memory-Dump Loop (UJ-5).");
            _playerStateEvents?.RaisePlayerDeath();

            if (_deathRoutine != null)
            {
                StopCoroutine(_deathRoutine);
            }
            _deathRoutine = StartCoroutine(MemoryDumpLoopRoutine());
        }

        private IEnumerator MemoryDumpLoopRoutine()
        {
            yield return new WaitForSeconds(_memoryDumpDuration);
            _memoryDumpResetEvents?.RaiseEvent();
            ResetState();
        }

        public void ResetState()
        {
            if (_deathRoutine != null)
            {
                StopCoroutine(_deathRoutine);
                _deathRoutine = null;
            }

            _currentHealth = _maxHealth;
            _isDead = false;
            _playerStateEvents?.RaiseHealthChanged(_currentHealth, _maxHealth);
            NullLog.Info("Health", "Player state reset.");
        }
    }
}
