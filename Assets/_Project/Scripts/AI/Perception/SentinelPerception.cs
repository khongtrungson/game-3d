using System;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.AI
{
    public enum AlertState
    {
        Unaware,      // Ambient idle / patrol
        Suspicious,   // Investigating acoustic stimulus / edge peek
        Alerted,      // In full combat pursuit / engaging player
        Suppressed    // Suppressed by smoke or heavy gunfire
    }

    /// <summary>
    /// AI Sentinel Perception Model (FR-22).
    /// Evaluates visual raycasts within a cone of 110° and 18m range.
    /// Intercepts acoustic stimulus spheres and respects LOSBlockerRegistry (Null-Cloud Smoke).
    /// Pre-allocates raycast hit buffers to guarantee zero per-frame garbage collection allocations.
    /// </summary>
    [DisallowMultipleComponent]
    public class SentinelPerception : MonoBehaviour
    {
        [Header("Visual Perception (FR-22)")]
        [Tooltip("Field of view cone in degrees (110°)")]
        [SerializeField] private float _fieldOfViewDegrees = 110f;

        [Tooltip("Maximum sight range in meters (18m)")]
        [SerializeField] private float _visualRange = 18f;

        [Tooltip("Eye origin transform for visual raycasts")]
        [SerializeField] private Transform _eyeTransform;

        [Tooltip("LayerMask for raycast sightline obstacles")]
        [SerializeField] private LayerMask _sightlineOcclusionMask = ~0;

        [Header("Acoustic Hearing (FR-22)")]
        [Tooltip("Hearing sensitivity multiplier (1.0 = standard)")]
        [SerializeField] private float _hearingSensitivity = 1.0f;

        [Tooltip("Event channel delivering acoustic stimuli from footsteps/gunfire")]
        [SerializeField] private AcousticStimulusEventChannelSO _acousticChannel;

        // Runtime state
        private AlertState _currentAlertState = AlertState.Unaware;
        private Vector3 _lastKnownThreatPosition;
        private Transform _currentTargetTransform;
        private bool _hasTargetInSight;
        private float _timeSinceLastSeenTarget = 999f;
        private readonly RaycastHit[] _raycastHits = new RaycastHit[16];

        public event Action<AlertState, AlertState> OnAlertStateChanged; // oldState, newState
        public event Action<Vector3, AcousticStimulusType> OnAcousticDetected;
        public event Action<Transform> OnTargetSpotted;
        public event Action OnTargetLost;

        public float FieldOfViewDegrees => _fieldOfViewDegrees;
        public float VisualRange => _visualRange;
        public AlertState CurrentAlertState => _currentAlertState;
        public Vector3 LastKnownThreatPosition => _lastKnownThreatPosition;
        public Transform CurrentTargetTransform => _currentTargetTransform;
        public bool HasTargetInSight => _hasTargetInSight;
        public float TimeSinceLastSeenTarget => _timeSinceLastSeenTarget;

        public Transform EyeTransform => _eyeTransform != null ? _eyeTransform : transform;

        private void Awake()
        {
            if (_eyeTransform == null)
            {
                _eyeTransform = transform;
            }
        }

        private void OnEnable()
        {
            if (_acousticChannel != null)
            {
                _acousticChannel.OnStimulusEmitted += HandleAcousticStimulus;
            }
        }

        private void OnDisable()
        {
            if (_acousticChannel != null)
            {
                _acousticChannel.OnStimulusEmitted -= HandleAcousticStimulus;
            }
        }

        private void Update()
        {
            _timeSinceLastSeenTarget += Time.deltaTime;
        }

        public void SetAcousticChannel(AcousticStimulusEventChannelSO channel)
        {
            if (_acousticChannel != null)
            {
                _acousticChannel.OnStimulusEmitted -= HandleAcousticStimulus;
            }
            _acousticChannel = channel;
            if (_acousticChannel != null && enabled)
            {
                _acousticChannel.OnStimulusEmitted += HandleAcousticStimulus;
            }
        }

        public void SetAlertState(AlertState newState)
        {
            if (_currentAlertState == newState) return;

            AlertState oldState = _currentAlertState;
            _currentAlertState = newState;
            NullLog.Info("AI_Perception", $"{gameObject.name} transition alert: {oldState} -> {newState}");
            OnAlertStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        /// Evaluates whether a target point is within the 110° FOV cone, <= 18m range,
        /// unobstructed by environmental geometry, and not occluded by Null-Cloud Smoke (LOSBlockerRegistry).
        /// </summary>
        public bool CanSeeTarget(Transform target, out Vector3 targetPosition)
        {
            targetPosition = Vector3.zero;
            if (target == null)
            {
                UpdateVisualSight(false, null);
                return false;
            }

            Vector3 eyePos = EyeTransform.position;
            Vector3 forward = EyeTransform.forward;
            Vector3 targetHead = target.position + Vector3.up * 1.5f; // eye height estimation
            Vector3 targetCenter = target.position + Vector3.up * 0.9f;

            // Check distance first
            float distToTarget = Vector3.Distance(eyePos, targetCenter);
            if (distToTarget > _visualRange)
            {
                UpdateVisualSight(false, target);
                return false;
            }

            // Check angle against forward vector (110° cone -> half-angle is 55°)
            Vector3 dirToTarget = (targetCenter - eyePos).normalized;
            float angle = Vector3.Angle(forward, dirToTarget);
            if (angle > (_fieldOfViewDegrees * 0.5f))
            {
                UpdateVisualSight(false, target);
                return false;
            }

            // Check LOS blockers (volumetric smoke)
            if (LOSBlockerRegistry.IsLOSBlocked(eyePos, targetCenter))
            {
                UpdateVisualSight(false, target);
                return false;
            }

            // Perform non-allocating physical raycast query against geometry
            int hitCount = Physics.RaycastNonAlloc(
                eyePos,
                dirToTarget,
                _raycastHits,
                distToTarget,
                _sightlineOcclusionMask,
                QueryTriggerInteraction.Ignore
            );

            bool lineOfSightClear = true;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = _raycastHits[i];
                // If hit collider is not belonging to target transform or its children
                if (hit.transform != target && !hit.transform.IsChildOf(target) && hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    lineOfSightClear = false;
                    break;
                }
            }

            if (lineOfSightClear)
            {
                targetPosition = targetCenter;
                _lastKnownThreatPosition = targetCenter;
                _timeSinceLastSeenTarget = 0f;
                UpdateVisualSight(true, target);
                return true;
            }

            // Try head raycast as fallback for peeking targets
            Vector3 dirToHead = (targetHead - eyePos).normalized;
            float headDist = Vector3.Distance(eyePos, targetHead);
            int headHitCount = Physics.RaycastNonAlloc(
                eyePos,
                dirToHead,
                _raycastHits,
                headDist,
                _sightlineOcclusionMask,
                QueryTriggerInteraction.Ignore
            );

            lineOfSightClear = true;
            for (int i = 0; i < headHitCount; i++)
            {
                var hit = _raycastHits[i];
                if (hit.transform != target && !hit.transform.IsChildOf(target) && hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    lineOfSightClear = false;
                    break;
                }
            }

            if (lineOfSightClear)
            {
                targetPosition = targetHead;
                _lastKnownThreatPosition = targetHead;
                _timeSinceLastSeenTarget = 0f;
                UpdateVisualSight(true, target);
                return true;
            }

            UpdateVisualSight(false, target);
            return false;
        }

        private void UpdateVisualSight(bool inSight, Transform target)
        {
            if (inSight)
            {
                if (!_hasTargetInSight)
                {
                    _hasTargetInSight = true;
                    _currentTargetTransform = target;
                    if (_currentAlertState != AlertState.Alerted)
                    {
                        SetAlertState(AlertState.Alerted);
                    }
                    OnTargetSpotted?.Invoke(target);
                }
            }
            else
            {
                if (_hasTargetInSight)
                {
                    _hasTargetInSight = false;
                    OnTargetLost?.Invoke();
                }
            }
        }

        /// <summary>
        /// Responds to acoustic stimuli broadcast via AcousticStimulusEventChannelSO.
        /// </summary>
        public void HandleAcousticStimulus(Vector3 origin, float radius, AcousticStimulusType type)
        {
            float dist = Vector3.Distance(transform.position, origin);
            float effectiveRadius = radius * _hearingSensitivity;

            if (dist <= effectiveRadius)
            {
                NullLog.Info("AI_Perception", $"{gameObject.name} detected acoustic noise {type} at distance {dist:F1}m (Radius: {effectiveRadius:F1}m)");
                _lastKnownThreatPosition = origin;

                if (_currentAlertState == AlertState.Unaware)
                {
                    SetAlertState(AlertState.Suspicious);
                }
                else if (_currentAlertState == AlertState.Suspicious)
                {
                    // Gunfire or repeated noise escalates to full alert
                    if (type == AcousticStimulusType.Gunfire || type == AcousticStimulusType.Impact)
                    {
                        SetAlertState(AlertState.Alerted);
                    }
                }

                OnAcousticDetected?.Invoke(origin, type);
            }
        }

        public void ForceAlert(Vector3 threatPos)
        {
            _lastKnownThreatPosition = threatPos;
            SetAlertState(AlertState.Alerted);
        }

        public void ResetPerception()
        {
            _currentAlertState = AlertState.Unaware;
            _lastKnownThreatPosition = Vector3.zero;
            _currentTargetTransform = null;
            _hasTargetInSight = false;
            _timeSinceLastSeenTarget = 999f;
        }
    }
}
