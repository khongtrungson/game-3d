using System;
using UnityEngine;
using UnityEngine.AI;
using NullProtocol.Core;

namespace NullProtocol.Gadgets
{
    /// <summary>
    /// Manages the 5.0s wireframe freeze debuff inflicted by Logic-Trip Mines (FR-21).
    /// Freezes the hostile into an immobile wireframe state where any weapon impact eliminates them instantly.
    /// </summary>
    public class FrozenHostileDebuff : MonoBehaviour, IFreezableHostile, IDamageable, IResettable
    {
        [Header("Freeze Settings (FR-21)")]
        [Tooltip("Freeze duration in seconds (5.0s)")]
        [SerializeField] private float _defaultFreezeDuration = 5.0f;

        // Runtime State
        private bool _isFrozen;
        private float _remainingFreezeTime;
        private NavMeshAgent _navAgent;
        private CharacterController _charController;
        private IDamageable _hostileDamageable;
        private Renderer[] _renderers;
        private bool _wasAgentStopped;

        public event Action OnFrozen;
        public event Action OnUnfrozen;

        public bool IsFrozen => _isFrozen;
        public float RemainingFreezeTime => _remainingFreezeTime;
        public int CurrentHealth => _hostileDamageable?.CurrentHealth ?? 1;
        public int MaxHealth => _hostileDamageable?.MaxHealth ?? 1;

        private void Awake()
        {
            _navAgent = GetComponentInParent<NavMeshAgent>();
            _charController = GetComponentInParent<CharacterController>();
            _hostileDamageable = GetComponentInParent<IDamageable>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        private void Update()
        {
            if (_isFrozen)
            {
                _remainingFreezeTime -= Time.deltaTime;
                if (_remainingFreezeTime <= 0f)
                {
                    Unfreeze();
                }
            }
        }

        public void ApplyWireframeFreeze(float duration)
        {
            _isFrozen = true;
            _remainingFreezeTime = (duration > 0f) ? duration : _defaultFreezeDuration;

            // Make hostile immobile (FR-21)
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _wasAgentStopped = _navAgent.isStopped;
                _navAgent.isStopped = true;
                _navAgent.velocity = Vector3.zero;
            }

            NullLog.Info("LogicTripMine", $"Hostile {gameObject.name} frozen into wireframe state for {_remainingFreezeTime:F1}s! (FR-21)");
            OnFrozen?.Invoke();
        }

        public void Unfreeze()
        {
            if (!_isFrozen) return;

            _isFrozen = false;
            _remainingFreezeTime = 0f;

            // Restore mobility
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = _wasAgentStopped;
            }

            NullLog.Info("LogicTripMine", $"Hostile {gameObject.name} wireframe freeze expired.");
            OnUnfrozen?.Invoke();
        }

        public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (!_isFrozen)
            {
                _hostileDamageable?.TakeDamage(amount, hitPoint, hitNormal, isHeadshot);
                return;
            }

            // FR-21: Instant elimination from ANY weapon impact while frozen
            if (amount > 0)
            {
                NullLog.Info("LogicTripMine", $"Frozen hostile {gameObject.name} hit by weapon impact! Instant shatter elimination! (FR-21)");
                if (_hostileDamageable != null)
                {
                    _hostileDamageable.TakeDamage(_hostileDamageable.MaxHealth * 10, hitPoint, hitNormal, true);
                }
                Unfreeze();
            }
        }

        public void ResetState()
        {
            _isFrozen = false;
            _remainingFreezeTime = 0f;
        }
    }
}
