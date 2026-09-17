using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NullProtocol.Core;
using NullProtocol.Combat;

namespace NullProtocol.AI
{
    /// <summary>
    /// Shield Breacher AI Archetype (FR-25).
    /// Specifications:
    /// - 120 HP base health pool.
    /// - Frontal 300 HP hard-light shield that blocks incoming frontal ballistics.
    /// - Advances deliberately on player positions (slow, steady pace).
    /// - Coordinates with companion Standard Sentinels to trigger flanking maneuvers around the player's flanks.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShieldBreacher : MonoBehaviour, IDamageable, ISentinelSmokeReaction, IResettable
    {
        [Header("Health & Shield (FR-25)")]
        [Tooltip("Body health pool (120 HP)")]
        [SerializeField] private int _maxBodyHealth = 120;

        [Tooltip("Frontal hard-light shield pool (300 HP)")]
        [SerializeField] private int _maxShieldHealth = 300;

        [Tooltip("Shield half-angle in degrees (e.g. 70° -> 140° frontal coverage)")]
        [SerializeField] private float _shieldHalfAngle = 70f;

        [Header("Locomotion & NavMesh")]
        [SerializeField] private NavMeshAgent _navAgent;
        [Tooltip("Deliberate advance speed (m/s)")]
        [SerializeField] private float _advanceSpeed = 1.6f;

        [Header("Perception & Audio")]
        [SerializeField] private SentinelPerception _perception;
        [SerializeField] private SentinelRadioChatter _radioChatter;
        [SerializeField] private KillEventChannelSO _killChannel;

        [Header("Squad Coordination (FR-25)")]
        [Tooltip("Companion sentinels assigned to coordinate flank attacks")]
        [SerializeField] private List<StandardSentinel> _companionSentinels = new List<StandardSentinel>();

        // Runtime State
        private int _currentBodyHealth;
        private int _currentShieldHealth;
        private bool _isShieldActive = true;
        private bool _isDead;
        private bool _isSuppressedBySmoke;
        private Transform _targetPlayer;
        private Vector3 _originalSpawnPosition;
        private Quaternion _originalSpawnRotation;
        private float _lastFlankOrderTime;

        public event Action<int, int> OnShieldDamaged; // currentShield, maxShield
        public event Action OnShieldBroken;
        public event Action<int, int> OnBodyDamaged; // currentBody, maxBody
        public event Action OnNeutralized;

        public int CurrentHealth => _currentBodyHealth;
        public int MaxHealth => _maxBodyHealth;
        public int CurrentBodyHealth => _currentBodyHealth;
        public int MaxBodyHealth => _maxBodyHealth;
        public int CurrentShieldHealth => _currentShieldHealth;
        public int MaxShieldHealth => _maxShieldHealth;
        public bool IsShieldActive => _isShieldActive;
        public bool IsDead => _isDead;
        public float AdvanceSpeed => _advanceSpeed;
        public bool IsSuppressedBySmoke => _isSuppressedBySmoke;
        public bool IsSeekingCover => false; // Shield breachers advance rather than hide behind cover

        private void Awake()
        {
            _currentBodyHealth = _maxBodyHealth;
            _currentShieldHealth = _maxShieldHealth;
            _originalSpawnPosition = transform.position;
            _originalSpawnRotation = transform.rotation;

            if (_navAgent == null) _navAgent = GetComponent<NavMeshAgent>();
            if (_perception == null) _perception = GetComponent<SentinelPerception>();
            if (_radioChatter == null) _radioChatter = GetComponent<SentinelRadioChatter>();

            if (_navAgent != null)
            {
                _navAgent.speed = _advanceSpeed;
            }
        }

        private void Start()
        {
            if (_targetPlayer == null)
            {
                var playerObj = GameObject.FindWithTag(Tags.Player);
                if (playerObj != null) _targetPlayer = playerObj.transform;
            }
        }

        private void Update()
        {
            if (_isDead) return;

            if (_targetPlayer != null)
            {
                // Deliberately advance on player position
                if (_navAgent != null && _navAgent.isOnNavMesh)
                {
                    _navAgent.isStopped = false;
                    _navAgent.speed = _advanceSpeed;
                    _navAgent.SetDestination(_targetPlayer.position);
                }

                // Coordinate companion flank maneuvers
                CoordinateCompanionFlanks();
            }
        }

        public void SetTargetPlayer(Transform player)
        {
            _targetPlayer = player;
        }

        public void RegisterCompanion(StandardSentinel companion)
        {
            if (companion != null && !_companionSentinels.Contains(companion))
            {
                _companionSentinels.Add(companion);
            }
        }

        public void CoordinateCompanionFlanks()
        {
            if (_companionSentinels.Count == 0 || _targetPlayer == null) return;

            // Coordinate flank order every 3.5 seconds
            if (Time.time - _lastFlankOrderTime < 3.5f) return;
            _lastFlankOrderTime = Time.time;

            _radioChatter?.EmitChatter(RadioChatterType.ShieldAdvancingCallout);

            Vector3 playerPos = _targetPlayer.position;
            Vector3 toBreacher = (transform.position - playerPos).normalized;
            Vector3 perp = Vector3.Cross(Vector3.up, toBreacher).normalized;

            for (int i = 0; i < _companionSentinels.Count; i++)
            {
                var sentinel = _companionSentinels[i];
                if (sentinel == null || sentinel.Health.IsDead) continue;

                // Alternate left and right flanking routes around player
                Vector3 flankOffset = (i % 2 == 0) ? (perp * 6.0f) : (-perp * 6.0f);
                Vector3 flankDestination = playerPos + flankOffset;

                sentinel.ExecuteFlankManeuver(flankDestination);
            }
        }

        public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (_isDead) return;

            // Check if impact hits frontal hard-light shield
            Vector3 toHitOrigin = -hitNormal; // or direction from which bullet arrived
            if (toHitOrigin == Vector3.zero) toHitOrigin = -transform.forward;

            float angleFromFront = Vector3.Angle(transform.forward, toHitOrigin);

            if (_isShieldActive && angleFromFront <= _shieldHalfAngle)
            {
                // Damage absorbed by frontal shield
                _currentShieldHealth -= amount;
                NullLog.Info("ShieldBreacher", $"Shield absorbed {amount} damage. Remaining Shield: {_currentShieldHealth}/{_maxShieldHealth}");
                OnShieldDamaged?.Invoke(_currentShieldHealth, _maxShieldHealth);

                if (_currentShieldHealth <= 0)
                {
                    _currentShieldHealth = 0;
                    _isShieldActive = false;
                    NullLog.Info("ShieldBreacher", "Shield shattered!");
                    OnShieldBroken?.Invoke();
                }
                return;
            }

            // Hit bypasses shield or shield is shattered -> impacts body
            _currentBodyHealth = Mathf.Max(0, _currentBodyHealth - amount);
            NullLog.Info("ShieldBreacher", $"Body hit for {amount} damage. Remaining Body HP: {_currentBodyHealth}/{_maxBodyHealth}");
            OnBodyDamaged?.Invoke(_currentBodyHealth, _maxBodyHealth);

            if (_currentBodyHealth <= 0)
            {
                Die(isHeadshot, hitPoint);
            }
        }

        private void Die(bool isHeadshot, Vector3 hitPoint)
        {
            if (_isDead) return;
            _isDead = true;

            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
            }

            OnNeutralized?.Invoke();
            _killChannel?.RaiseKill(this, isHeadshot, hitPoint);
        }

        #region Smoke Reaction (FR-19)

        public void OnSmokeSuppressed(Vector3 smokeCenter, float smokeRadius)
        {
            _isSuppressedBySmoke = true;
            _radioChatter?.EmitChatter(RadioChatterType.SuppressionCallout);
        }

        public void OnSmokeCleared()
        {
            _isSuppressedBySmoke = false;
        }

        #endregion

        #region IResettable Implementation

        public void ResetState()
        {
            transform.SetPositionAndRotation(_originalSpawnPosition, _originalSpawnRotation);
            _currentBodyHealth = _maxBodyHealth;
            _currentShieldHealth = _maxShieldHealth;
            _isShieldActive = true;
            _isDead = false;
            _isSuppressedBySmoke = false;
            _lastFlankOrderTime = -99f;

            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.ResetPath();
                _navAgent.isStopped = true;
            }

            _perception?.ResetPerception();
        }

        #endregion
    }
}
