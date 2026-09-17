using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using NullProtocol.Core;
using NullProtocol.Combat;
using NullProtocol.Gadgets;

namespace NullProtocol.AI
{
    /// <summary>
    /// Null-01 Mirror Operative Boss AI (FR-26).
    /// Specifications:
    /// - 250 HP boss health pool.
    /// - Peeks corners/cover to deliver precision bursts.
    /// - Throws defensive Null-Cloud Smoke when under heavy suppression / sight loss.
    /// - Deploys Hard-Light Barricade when HP falls below 50% (<= 125 HP).
    /// - Executes aggressive dynamic flanking maneuvers.
    /// </summary>
    [DisallowMultipleComponent]
    public class Null01Boss : MonoBehaviour, IDamageable, ISentinelSmokeReaction, IResettable
    {
        [Header("Boss Health Settings (FR-26)")]
        [Tooltip("Null-01 Mirror Operative health (250 HP)")]
        [SerializeField] private int _maxHealth = 250;

        [Header("Gadget Prefabs & Spawning (FR-26)")]
        [SerializeField] private DeployableBarricade _barricadePrefab;
        [SerializeField] private NullCloudSmoke _smokePrefab;

        [Header("Perception & Audio")]
        [SerializeField] private SentinelPerception _perception;
        [SerializeField] private SentinelRadioChatter _radioChatter;
        [SerializeField] private KillEventChannelSO _killChannel;

        [Header("Locomotion & Combat")]
        [SerializeField] private NavMeshAgent _navAgent;
        [SerializeField] private float _baseSpeed = 3.5f;
        [SerializeField] private float _flankSpeed = 5.0f;
        [SerializeField] private int _burstDamage = 30;
        [SerializeField] private int _burstRounds = 3;

        // Runtime State
        private int _currentHealth;
        private bool _isDead;
        private bool _hasDeployedLowHealthBarricade;
        private bool _hasThrownDefensiveSmoke;
        private bool _isPeeking;
        private bool _isFlanking;
        private bool _isSuppressedBySmoke;
        private Transform _targetPlayer;
        private Vector3 _originalSpawnPosition;
        private Quaternion _originalSpawnRotation;
        private CoverNode _currentCoverNode;
        private DeployableBarricade _activeBarricadeInstance;
        private NullCloudSmoke _activeSmokeInstance;
        private Coroutine _burstRoutine;

        public event Action<int, int> OnHealthChanged; // current, max
        public event Action OnBarricadeDeployed;
        public event Action OnSmokeThrown;
        public event Action OnBossDefeated;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsDead => _isDead;
        public bool HasDeployedLowHealthBarricade => _hasDeployedLowHealthBarricade;
        public bool HasThrownDefensiveSmoke => _hasThrownDefensiveSmoke;
        public bool IsPeeking => _isPeeking;
        public bool IsFlanking => _isFlanking;
        public bool IsSuppressedBySmoke => _isSuppressedBySmoke;
        public bool IsSeekingCover => false;

        private void Awake()
        {
            _currentHealth = _maxHealth;
            _originalSpawnPosition = transform.position;
            _originalSpawnRotation = transform.rotation;

            if (_navAgent == null) _navAgent = GetComponent<NavMeshAgent>();
            if (_perception == null) _perception = GetComponent<SentinelPerception>();
            if (_radioChatter == null) _radioChatter = GetComponent<SentinelRadioChatter>();

            if (_navAgent != null)
            {
                _navAgent.speed = _baseSpeed;
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

        public void SetTargetPlayer(Transform player)
        {
            _targetPlayer = player;
        }

        public void SetGadgetPrefabs(DeployableBarricade barricade, NullCloudSmoke smoke)
        {
            _barricadePrefab = barricade;
            _smokePrefab = smoke;
        }

        public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (_isDead) return;

            // Boss takes standard or headshot damage (with headshot bonus)
            int effectiveDamage = isHeadshot ? Mathf.RoundToInt(amount * 1.5f) : amount;
            _currentHealth = Mathf.Max(0, _currentHealth - effectiveDamage);

            NullLog.Info("Null01Boss", $"Null-01 took {effectiveDamage} damage. HP: {_currentHealth}/{_maxHealth}");
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            // FR-26: Deploy Hard-Light Barricade when HP falls below 50% (<= 125 HP)
            if (!_hasDeployedLowHealthBarricade && _currentHealth <= (_maxHealth * 0.5f))
            {
                DeployDefensiveBarricade();
            }

            if (_currentHealth <= 0)
            {
                Die(isHeadshot, hitPoint);
            }
            else if (_currentHealth < 100 && !_hasThrownDefensiveSmoke)
            {
                // Under heavy fire / low HP -> throw defensive smoke
                ThrowDefensiveSmoke();
            }
        }

        /// <summary>
        /// Deploys a Hard-Light Barricade directly between boss and threat (FR-26).
        /// </summary>
        public void DeployDefensiveBarricade()
        {
            if (_hasDeployedLowHealthBarricade) return;
            _hasDeployedLowHealthBarricade = true;

            _radioChatter?.EmitChatter(RadioChatterType.CoverRelocatingCallout, "Sub-50% integrity! Deploying hard-light barricade!");

            Vector3 deployPos = transform.position + transform.forward * 1.5f;
            Quaternion deployRot = Quaternion.LookRotation(transform.forward);

            if (_barricadePrefab != null)
            {
                _activeBarricadeInstance = Instantiate(_barricadePrefab, deployPos, deployRot);
                _activeBarricadeInstance.DeployImmediate(deployPos, deployRot);
            }

            OnBarricadeDeployed?.Invoke();

            // Immediately initiate aggressive flank maneuver
            ExecuteAggressiveFlank();
        }

        /// <summary>
        /// Throws defensive Null-Cloud Smoke to obscure LOS and facilitate relocation (FR-26).
        /// </summary>
        public void ThrowDefensiveSmoke()
        {
            if (_hasThrownDefensiveSmoke) return;
            _hasThrownDefensiveSmoke = true;

            _radioChatter?.EmitChatter(RadioChatterType.SuppressionCallout, "Visual obscured! Deploying Null-Cloud obscurant!");

            Vector3 smokePos = transform.position + transform.forward * 2.0f;
            if (_smokePrefab != null)
            {
                _activeSmokeInstance = Instantiate(_smokePrefab, smokePos, Quaternion.identity);
                _activeSmokeInstance.Detonate(smokePos);
            }

            OnSmokeThrown?.Invoke();
            ExecuteAggressiveFlank();
        }

        /// <summary>
        /// Executes an aggressive flanking maneuver around player (FR-26).
        /// </summary>
        public void ExecuteAggressiveFlank()
        {
            _isFlanking = true;
            _radioChatter?.EmitChatter(RadioChatterType.FlankingCallout, "Executing aggressive perimeter flank!");

            if (_targetPlayer != null)
            {
                Vector3 playerPos = _targetPlayer.position;
                Vector3 toPlayer = (playerPos - transform.position).normalized;
                Vector3 flankPerp = Vector3.Cross(Vector3.up, toPlayer).normalized;

                // Pick wide flank anchor (8m out)
                Vector3 flankDest = playerPos + flankPerp * 8.0f;

                if (_navAgent != null && _navAgent.isOnNavMesh)
                {
                    _navAgent.isStopped = false;
                    _navAgent.speed = _flankSpeed;
                    _navAgent.SetDestination(flankDest);
                }
            }
        }

        /// <summary>
        /// Delivers corner peeking 3-round burst fire.
        /// </summary>
        public void PeekAndFireBurst(Vector3 targetPos, Action onBurstComplete = null)
        {
            _isPeeking = true;
            if (_burstRoutine != null) StopCoroutine(_burstRoutine);
            _burstRoutine = StartCoroutine(BurstRoutine(targetPos, onBurstComplete));
        }

        private IEnumerator BurstRoutine(Vector3 targetPos, Action onBurstComplete)
        {
            Vector3 origin = transform.position + Vector3.up * 1.6f;
            for (int i = 0; i < _burstRounds; i++)
            {
                Vector3 dir = (targetPos - origin).normalized;
                if (Physics.Raycast(origin, dir, out RaycastHit hit, 40f, Layers.HitscanTargets, QueryTriggerInteraction.Ignore))
                {
                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        bool isHeadshot = hit.collider.CompareTag(Tags.EnemyHead);
                        damageable.TakeDamage(_burstDamage, hit.point, hit.normal, isHeadshot);
                    }
                }
                yield return new WaitForSeconds(0.1f);
            }

            _isPeeking = false;
            _burstRoutine = null;
            onBurstComplete?.Invoke();
        }

        private void Die(bool isHeadshot, Vector3 hitPoint)
        {
            if (_isDead) return;
            _isDead = true;

            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
            }

            NullLog.Info("Null01Boss", "Null-01 Mirror Operative defeated!");
            OnBossDefeated?.Invoke();
            _killChannel?.RaiseKill(this, isHeadshot, hitPoint);
        }

        #region Smoke Reaction (FR-19)

        public void OnSmokeSuppressed(Vector3 smokeCenter, float smokeRadius)
        {
            _isSuppressedBySmoke = true;
            // Mirror operative is an elite boss: reacts by aggressively flanking out of smoke
            ExecuteAggressiveFlank();
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
            _currentHealth = _maxHealth;
            _isDead = false;
            _hasDeployedLowHealthBarricade = false;
            _hasThrownDefensiveSmoke = false;
            _isPeeking = false;
            _isFlanking = false;
            _isSuppressedBySmoke = false;

            if (_burstRoutine != null)
            {
                StopCoroutine(_burstRoutine);
                _burstRoutine = null;
            }

            if (_activeBarricadeInstance != null)
            {
                Destroy(_activeBarricadeInstance.gameObject);
                _activeBarricadeInstance = null;
            }

            if (_activeSmokeInstance != null)
            {
                Destroy(_activeSmokeInstance.gameObject);
                _activeSmokeInstance = null;
            }

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
