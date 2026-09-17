using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using NullProtocol.Core;
using NullProtocol.Combat;

namespace NullProtocol.AI
{
    /// <summary>
    /// Standard Sentinel AI Tactical Agent (FR-22, FR-23, FR-24, FR-27).
    /// Features:
    /// - 110° FOV / 18m visual perception & acoustic stimulus detection.
    /// - Ambient cover node queries, peeking (left/right/over) for 3-round rifle bursts, crouching to reload.
    /// - Doorway stacking protocols for synchronized room breaches.
    /// - Dynamic flanking routes when player is suppressed or behind deployable cover.
    /// - Synthetic 3D spatialized radio chatter callouts.
    /// - Full integration with NullCloudSmoke (ISentinelSmokeReaction) and LogicTripMine (IFreezableHostile).
    /// </summary>
    [DisallowMultipleComponent]
    public class StandardSentinel : MonoBehaviour, ISentinelSmokeReaction, IResettable
    {
        [Header("Perception & Radio")]
        [SerializeField] private SentinelPerception _perception;
        [SerializeField] private SentinelRadioChatter _radioChatter;
        [SerializeField] private SentinelHealth _health;

        [Header("Locomotion & NavMesh")]
        [SerializeField] private NavMeshAgent _navAgent;
        [SerializeField] private float _walkSpeed = 2.5f;
        [SerializeField] private float _flankSpeed = 4.0f;

        [Header("Weapon & Firing (FR-23)")]
        [Tooltip("Damage per bullet in burst")]
        [SerializeField] private int _bulletDamage = 25;
        [Tooltip("Rounds per burst (3 rounds)")]
        [SerializeField] private int _burstCount = 3;
        [Tooltip("Delay between shots in burst (seconds)")]
        [SerializeField] private float _burstInterval = 0.12f;
        [Tooltip("Delay between bursts while peeking (seconds)")]
        [SerializeField] private float _burstCooldown = 1.0f;
        [Tooltip("Time to crouch reload behind cover (seconds)")]
        [SerializeField] private float _reloadDuration = 2.0f;
        [Tooltip("Total magazine capacity before requiring reload")]
        [SerializeField] private int _magazineCapacity = 9;

        [Header("Cover & Tactics")]
        [Tooltip("Maximum distance to query cover nodes (meters)")]
        [SerializeField] private float _coverQueryRadius = 12.0f;
        [Tooltip("Duration of each peek window (seconds)")]
        [SerializeField] private float _peekDuration = 1.5f;

        // Runtime State
        private SentinelStateMachine _stateMachine;
        private CoverNode _currentCoverNode;
        private PeekDirection _currentPeekDirection = PeekDirection.None;
        private DoorwayStackingCoordinator _assignedDoorCoordinator;
        private StackingSide _assignedStackSide;
        private Transform _targetPlayer;
        private int _currentAmmo;
        private bool _isCrouching;
        private bool _isPeeking;
        private bool _isFlanking;
        private bool _isSuppressedBySmoke;
        private bool _isSeekingCover;
        private Vector3 _originalSpawnPosition;
        private Quaternion _originalSpawnRotation;
        private Coroutine _burstRoutine;

        public event Action OnBurstFired;
        public event Action OnReloadStarted;
        public event Action OnReloadFinished;

        public SentinelPerception Perception => _perception;
        public SentinelRadioChatter RadioChatter => _radioChatter;
        public SentinelHealth Health => _health;
        public NavMeshAgent NavAgent => _navAgent;
        public CoverNode CurrentCoverNode => _currentCoverNode;
        public PeekDirection CurrentPeekDirection => _currentPeekDirection;
        public int CurrentAmmo => _currentAmmo;
        public int MagazineCapacity => _magazineCapacity;
        public bool IsCrouching => _isCrouching;
        public bool IsPeeking => _isPeeking;
        public bool IsFlanking => _isFlanking;
        public bool IsSuppressedBySmoke => _isSuppressedBySmoke;
        public bool IsSeekingCover => _isSeekingCover;
        public string CurrentStateName => _stateMachine != null ? _stateMachine.CurrentStateName : "Uninitialized";

        private void Awake()
        {
            if (_perception == null) _perception = GetComponent<SentinelPerception>();
            if (_radioChatter == null) _radioChatter = GetComponent<SentinelRadioChatter>();
            if (_health == null) _health = GetComponent<SentinelHealth>();
            if (_navAgent == null) _navAgent = GetComponent<NavMeshAgent>();

            _originalSpawnPosition = transform.position;
            _originalSpawnRotation = transform.rotation;
            _currentAmmo = _magazineCapacity;

            InitializeStateMachine();
        }

        private void Start()
        {
            // Auto-locate player if not assigned
            if (_targetPlayer == null)
            {
                var playerObj = GameObject.FindWithTag(Tags.Player);
                if (playerObj != null) _targetPlayer = playerObj.transform;
            }
        }

        private void OnEnable()
        {
            if (_perception != null)
            {
                _perception.OnAlertStateChanged += HandleAlertStateChanged;
                _perception.OnTargetSpotted += HandleTargetSpotted;
            }
        }

        private void OnDisable()
        {
            if (_perception != null)
            {
                _perception.OnAlertStateChanged -= HandleAlertStateChanged;
                _perception.OnTargetSpotted -= HandleTargetSpotted;
            }

            ReleaseCoverNode();
        }

        private void Update()
        {
            if (_health != null && (_health.IsDead || _health.IsFrozen))
            {
                if (_navAgent != null && _navAgent.isOnNavMesh && !_navAgent.isStopped)
                {
                    _navAgent.isStopped = true;
                }
                return;
            }

            // Periodic player perception check
            if (_targetPlayer != null && _perception != null)
            {
                _perception.CanSeeTarget(_targetPlayer, out _);
            }

            _stateMachine?.Update();
        }

        private void FixedUpdate()
        {
            if (_health != null && (_health.IsDead || _health.IsFrozen)) return;
            _stateMachine?.FixedUpdate();
        }

        private void InitializeStateMachine()
        {
            _stateMachine = new SentinelStateMachine();
            _stateMachine.ChangeState(new SentinelIdleState(this));
        }

        public void SetTargetPlayer(Transform player)
        {
            _targetPlayer = player;
        }

        public Transform GetTargetPlayer() => _targetPlayer;

        public void SetNavAgent(NavMeshAgent agent)
        {
            _navAgent = agent;
        }

        public void SetPerception(SentinelPerception perception)
        {
            _perception = perception;
        }

        public void SetRadioChatter(SentinelRadioChatter chatter)
        {
            _radioChatter = chatter;
        }

        public void SetHealth(SentinelHealth health)
        {
            _health = health;
        }

        #region State Machine Actions

        public void TransitionToState(IState newState)
        {
            _stateMachine.ChangeState(newState);
        }

        private void HandleAlertStateChanged(AlertState oldState, AlertState newState)
        {
            if (newState == AlertState.Alerted)
            {
                _radioChatter?.EmitChatter(RadioChatterType.AlertStatusChanged);
                if (_stateMachine.CurrentState is SentinelIdleState)
                {
                    TransitionToCoverOrCombat();
                }
            }
            else if (newState == AlertState.Suppressed)
            {
                _radioChatter?.EmitChatter(RadioChatterType.SuppressionCallout);
                TransitionToCoverOrCombat();
            }
        }

        private void HandleTargetSpotted(Transform target)
        {
            _targetPlayer = target;
            if (_stateMachine.CurrentState is SentinelIdleState)
            {
                TransitionToCoverOrCombat();
            }
        }

        public void TransitionToCoverOrCombat()
        {
            Vector3 threatPos = _perception != null && _perception.LastKnownThreatPosition != Vector3.zero
                ? _perception.LastKnownThreatPosition
                : (_targetPlayer != null ? _targetPlayer.position : transform.position + transform.forward * 5f);

            var bestCover = CoverNodeRegistry.FindBestCoverNode(transform.position, threatPos, _coverQueryRadius, _currentCoverNode);
            if (bestCover != null && bestCover.ClaimOccupancy(this))
            {
                ReleaseCoverNode();
                _currentCoverNode = bestCover;
                _isSeekingCover = true;
                _radioChatter?.EmitChatter(RadioChatterType.CoverRelocatingCallout);
                TransitionToState(new SentinelSeekCoverState(this, _currentCoverNode));
            }
            else
            {
                _isSeekingCover = false;
                TransitionToState(new SentinelCombatState(this));
            }
        }

        public void ExecuteFlankManeuver(Vector3 flankAnchor)
        {
            _isFlanking = true;
            _radioChatter?.EmitChatter(RadioChatterType.FlankingCallout);
            TransitionToState(new SentinelFlankState(this, flankAnchor));
        }

        public void AssignDoorwayStack(DoorwayStackingCoordinator coordinator, StackingSide side)
        {
            _assignedDoorCoordinator = coordinator;
            _assignedStackSide = side;
            _radioChatter?.EmitChatter(RadioChatterType.DoorStackCallout);
            TransitionToState(new SentinelDoorStackState(this, coordinator, side));
        }

        #endregion

        #region Cover, Peeking & Firing (FR-23)

        public void SetCrouching(bool crouching)
        {
            _isCrouching = crouching;
        }

        public void SetPeeking(bool peeking, PeekDirection direction)
        {
            _isPeeking = peeking;
            _currentPeekDirection = direction;
        }

        public void Fire3RoundBurst(Vector3 targetPos, Action onBurstComplete = null)
        {
            if (_burstRoutine != null)
            {
                StopCoroutine(_burstRoutine);
            }
            _burstRoutine = StartCoroutine(BurstRoutine(targetPos, onBurstComplete));
        }

        private IEnumerator BurstRoutine(Vector3 targetPos, Action onBurstComplete)
        {
            int shotsToFire = Mathf.Min(_burstCount, _currentAmmo);
            for (int i = 0; i < shotsToFire; i++)
            {
                ExecuteSingleShot(targetPos);
                _currentAmmo--;
                yield return new WaitForSeconds(_burstInterval);
            }

            OnBurstFired?.Invoke();
            _burstRoutine = null;
            onBurstComplete?.Invoke();
        }

        private void ExecuteSingleShot(Vector3 targetPos)
        {
            Vector3 fireOrigin = _perception != null ? _perception.EyeTransform.position : transform.position + Vector3.up * 1.5f;
            Vector3 fireDir = (targetPos - fireOrigin).normalized;

            if (Physics.Raycast(fireOrigin, fireDir, out RaycastHit hit, 30f, Layers.HitscanTargets, QueryTriggerInteraction.Ignore))
            {
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    bool isHeadshot = hit.collider.CompareTag(Tags.EnemyHead);
                    damageable.TakeDamage(_bulletDamage, hit.point, hit.normal, isHeadshot);
                }
            }
        }

        public void PerformReload(Action onReloadComplete = null)
        {
            StartCoroutine(ReloadRoutine(onReloadComplete));
        }

        private IEnumerator ReloadRoutine(Action onReloadComplete)
        {
            OnReloadStarted?.Invoke();
            SetCrouching(true);
            yield return new WaitForSeconds(_reloadDuration);
            _currentAmmo = _magazineCapacity;
            OnReloadFinished?.Invoke();
            onReloadComplete?.Invoke();
        }

        public void ReleaseCoverNode()
        {
            if (_currentCoverNode != null)
            {
                _currentCoverNode.ReleaseOccupancy(this);
                _currentCoverNode = null;
            }
        }

        #endregion

        #region Smoke Reaction (FR-19)

        public void OnSmokeSuppressed(Vector3 smokeCenter, float smokeRadius)
        {
            _isSuppressedBySmoke = true;
            _isSeekingCover = true;
            _radioChatter?.EmitChatter(RadioChatterType.SuppressionCallout);

            // Cease firing and seek defensive cover away from smoke
            if (_burstRoutine != null)
            {
                StopCoroutine(_burstRoutine);
                _burstRoutine = null;
            }

            TransitionToCoverOrCombat();
        }

        public void OnSmokeCleared()
        {
            _isSuppressedBySmoke = false;
            _isSeekingCover = false;
        }

        #endregion

        #region IResettable Implementation

        public void ResetState()
        {
            ReleaseCoverNode();
            if (_burstRoutine != null)
            {
                StopCoroutine(_burstRoutine);
                _burstRoutine = null;
            }

            transform.SetPositionAndRotation(_originalSpawnPosition, _originalSpawnRotation);
            _currentAmmo = _magazineCapacity;
            _isCrouching = false;
            _isPeeking = false;
            _isFlanking = false;
            _isSuppressedBySmoke = false;
            _isSeekingCover = false;
            _currentPeekDirection = PeekDirection.None;

            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.ResetPath();
                _navAgent.isStopped = true;
            }

            _perception?.ResetPerception();
            _health?.ResetState();
            InitializeStateMachine();
        }

        #endregion
    }

    #region HFSM Concrete States

    public class SentinelIdleState : IState
    {
        private readonly StandardSentinel _sentinel;

        public SentinelIdleState(StandardSentinel sentinel)
        {
            _sentinel = sentinel;
        }

        public void Enter()
        {
            if (_sentinel.NavAgent != null && _sentinel.NavAgent.isOnNavMesh)
            {
                _sentinel.NavAgent.isStopped = true;
            }
        }

        public void Update() { }
        public void FixedUpdate() { }
        public void Exit() { }
    }

    public class SentinelSeekCoverState : IState
    {
        private readonly StandardSentinel _sentinel;
        private readonly CoverNode _node;

        public SentinelSeekCoverState(StandardSentinel sentinel, CoverNode node)
        {
            _sentinel = sentinel;
            _node = node;
        }

        public void Enter()
        {
            if (_sentinel.NavAgent != null && _sentinel.NavAgent.isOnNavMesh)
            {
                _sentinel.NavAgent.isStopped = false;
                _sentinel.NavAgent.SetDestination(_node.Position);
            }
        }

        public void Update()
        {
            if (_sentinel.NavAgent != null && _sentinel.NavAgent.isOnNavMesh)
            {
                if (!_sentinel.NavAgent.pathPending && _sentinel.NavAgent.remainingDistance <= 0.6f)
                {
                    _sentinel.TransitionToState(new SentinelPeekAndFireState(_sentinel, _node));
                }
            }
            else
            {
                // Fallback for EditMode tests or environments without active NavMesh
                float dist = Vector3.Distance(_sentinel.transform.position, _node.Position);
                if (dist <= 0.6f)
                {
                    _sentinel.TransitionToState(new SentinelPeekAndFireState(_sentinel, _node));
                }
            }
        }

        public void FixedUpdate() { }
        public void Exit() { }
    }

    public class SentinelPeekAndFireState : IState
    {
        private readonly StandardSentinel _sentinel;
        private readonly CoverNode _node;
        private float _stateTimer;
        private bool _isPeekingOut;

        public SentinelPeekAndFireState(StandardSentinel sentinel, CoverNode node)
        {
            _sentinel = sentinel;
            _node = node;
        }

        public void Enter()
        {
            _sentinel.SetCrouching(true);
            _isPeekingOut = false;
            _stateTimer = 0.5f; // Crouch preparation time
        }

        public void Update()
        {
            _stateTimer -= Time.deltaTime;

            if (_sentinel.CurrentAmmo <= 0)
            {
                // Must reload behind cover
                _sentinel.SetCrouching(true);
                _sentinel.SetPeeking(false, PeekDirection.None);
                _sentinel.PerformReload(() => {
                    _stateTimer = 0.2f;
                });
                return;
            }

            if (_stateTimer <= 0f)
            {
                if (!_isPeekingOut)
                {
                    // Peek out
                    _isPeekingOut = true;
                    _sentinel.SetCrouching(false);

                    PeekDirection dir = _node.CanPeekRight ? PeekDirection.Right : (_node.CanPeekLeft ? PeekDirection.Left : PeekDirection.Over);
                    _sentinel.SetPeeking(true, dir);

                    Vector3 targetPos = _sentinel.GetTargetPlayer() != null
                        ? _sentinel.GetTargetPlayer().position
                        : _sentinel.transform.position + _sentinel.transform.forward * 10f;

                    _sentinel.Fire3RoundBurst(targetPos, () => {
                        // After burst, return to crouch
                        _sentinel.SetPeeking(false, PeekDirection.None);
                        _sentinel.SetCrouching(true);
                        _isPeekingOut = false;
                        _stateTimer = 1.0f; // Duck interval
                    });

                    _stateTimer = 2.0f;
                }
            }
        }

        public void FixedUpdate() { }
        public void Exit()
        {
            _sentinel.SetPeeking(false, PeekDirection.None);
            _sentinel.SetCrouching(false);
        }
    }

    public class SentinelCombatState : IState
    {
        private readonly StandardSentinel _sentinel;
        private float _burstTimer = 0.5f;

        public SentinelCombatState(StandardSentinel sentinel)
        {
            _sentinel = sentinel;
        }

        public void Enter()
        {
            if (_sentinel.NavAgent != null && _sentinel.NavAgent.isOnNavMesh)
            {
                _sentinel.NavAgent.isStopped = true;
            }
        }

        public void Update()
        {
            _burstTimer -= Time.deltaTime;
            if (_burstTimer <= 0f && _sentinel.CurrentAmmo > 0)
            {
                _burstTimer = 1.2f;
                Vector3 targetPos = _sentinel.GetTargetPlayer() != null
                    ? _sentinel.GetTargetPlayer().position
                    : _sentinel.transform.position + _sentinel.transform.forward * 8f;
                _sentinel.Fire3RoundBurst(targetPos);
            }
            else if (_sentinel.CurrentAmmo <= 0)
            {
                _sentinel.PerformReload(() => _burstTimer = 0.5f);
            }
        }

        public void FixedUpdate() { }
        public void Exit() { }
    }

    public class SentinelFlankState : IState
    {
        private readonly StandardSentinel _sentinel;
        private readonly Vector3 _flankDestination;

        public SentinelFlankState(StandardSentinel sentinel, Vector3 flankDestination)
        {
            _sentinel = sentinel;
            _flankDestination = flankDestination;
        }

        public void Enter()
        {
            if (_sentinel.NavAgent != null && _sentinel.NavAgent.isOnNavMesh)
            {
                _sentinel.NavAgent.isStopped = false;
                _sentinel.NavAgent.speed = 4.0f;
                _sentinel.NavAgent.SetDestination(_flankDestination);
            }
        }

        public void Update()
        {
            float dist = Vector3.Distance(_sentinel.transform.position, _flankDestination);
            if (dist <= 1.0f)
            {
                _sentinel.TransitionToCoverOrCombat();
            }
        }

        public void FixedUpdate() { }
        public void Exit()
        {
            if (_sentinel.NavAgent != null && _sentinel.NavAgent.isOnNavMesh)
            {
                _sentinel.NavAgent.speed = 2.5f;
            }
        }
    }

    public class SentinelDoorStackState : IState
    {
        private readonly StandardSentinel _sentinel;
        private readonly DoorwayStackingCoordinator _coordinator;
        private readonly StackingSide _side;
        private bool _isStacked;

        public SentinelDoorStackState(StandardSentinel sentinel, DoorwayStackingCoordinator coordinator, StackingSide side)
        {
            _sentinel = sentinel;
            _coordinator = coordinator;
            _side = side;
        }

        public void Enter()
        {
            Vector3 stackPos = _coordinator.GetStackPosition(_side);
            if (_sentinel.NavAgent != null && _sentinel.NavAgent.isOnNavMesh)
            {
                _sentinel.NavAgent.isStopped = false;
                _sentinel.NavAgent.SetDestination(stackPos);
            }

            _coordinator.OnBreachExecuted += HandleBreach;
        }

        public void Update()
        {
            if (!_isStacked)
            {
                Vector3 stackPos = _coordinator.GetStackPosition(_side);
                float dist = Vector3.Distance(_sentinel.transform.position, stackPos);
                if (dist <= 0.8f)
                {
                    _isStacked = true;
                    _sentinel.RadioChatter?.EmitChatter(RadioChatterType.DoorStackCallout);
                    _coordinator.ReportReady(_sentinel);
                }
            }
        }

        private void HandleBreach()
        {
            _sentinel.RadioChatter?.EmitChatter(RadioChatterType.BreachingCallout);
            Vector3 breachDest = _coordinator.GetBreachDestination(_side);

            if (_sentinel.NavAgent != null && _sentinel.NavAgent.isOnNavMesh)
            {
                _sentinel.NavAgent.isStopped = false;
                _sentinel.NavAgent.SetDestination(breachDest);
            }

            _sentinel.TransitionToState(new SentinelCombatState(_sentinel));
        }

        public void FixedUpdate() { }

        public void Exit()
        {
            _coordinator.OnBreachExecuted -= HandleBreach;
            _coordinator.ReleaseStacker(_sentinel);
        }
    }

    #endregion
}
