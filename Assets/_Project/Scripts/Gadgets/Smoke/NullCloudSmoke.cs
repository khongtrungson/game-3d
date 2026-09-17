using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Gadgets
{
    /// <summary>
    /// Null-Cloud Smoke deployable obscurant (FR-18, FR-19).
    /// Detonates on impact into a 4.0m volumetric digital static field for 8.0s.
    /// Interrupts AI line-of-sight raycasts and suppresses sentinels (cease firing, seek cover).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class NullCloudSmoke : MonoBehaviour, ILOSBlocker, IResettable
    {
        [Header("Volumetric Field Settings (FR-18)")]
        [Tooltip("Radius of digital static field in meters (4.0m)")]
        [SerializeField] private float _fieldRadius = 4.0f;
        [Tooltip("Active lifetime in seconds (8.0s)")]
        [SerializeField] private float _fieldDuration = 8.0f;

        [Header("Components")]
        [SerializeField] private SphereCollider _triggerCollider;

        // Runtime state
        private bool _isActive;
        private float _remainingDuration;
        private readonly HashSet<ISentinelSmokeReaction> _suppressedSentinels = new HashSet<ISentinelSmokeReaction>();
        private Coroutine _lifetimeCoroutine;

        public event Action<NullCloudSmoke> OnSmokeDetonated;
        public event Action<NullCloudSmoke> OnSmokeDissipated;

        public bool IsActive => _isActive;
        public Vector3 Center => transform.position;
        public float Radius => _fieldRadius;
        public float Duration => _fieldDuration;
        public float RemainingDuration => _remainingDuration;
        public IReadOnlyCollection<ISentinelSmokeReaction> SuppressedSentinels => _suppressedSentinels;

        private void Awake()
        {
            if (_triggerCollider == null)
            {
                _triggerCollider = GetComponent<SphereCollider>();
            }

            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
                _triggerCollider.radius = _fieldRadius;
            }
        }

        private void OnEnable()
        {
            if (_isActive)
            {
                LOSBlockerRegistry.Register(this);
            }
        }

        private void OnDisable()
        {
            LOSBlockerRegistry.Unregister(this);
            ReleaseAllSuppressedSentinels();
        }

        /// <summary>
        /// Detonates the static field at the given position (FR-18).
        /// </summary>
        public void Detonate(Vector3 position)
        {
            transform.position = position;
            _isActive = true;
            _remainingDuration = _fieldDuration;
            _suppressedSentinels.Clear();

            gameObject.SetActive(true);
            LOSBlockerRegistry.Register(this);

            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
            }
            _lifetimeCoroutine = StartCoroutine(LifetimeRoutine());

            // Immediate scan for sentinels inside radius
            EvaluateSuppressionSphere();

            NullLog.Info("NullCloudSmoke", $"Null-Cloud Smoke detonated at {position}. Volumetric static field active for {_fieldDuration:F1}s (Radius: {_fieldRadius:F1}m)");
            OnSmokeDetonated?.Invoke(this);
        }

        private IEnumerator LifetimeRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _fieldDuration)
            {
                elapsed += Time.deltaTime;
                _remainingDuration = Mathf.Max(0f, _fieldDuration - elapsed);

                // Continuously evaluate suppression for sentinels entering or caught
                EvaluateSuppressionSphere();

                yield return null;
            }

            Dissipate();
        }

        public void Dissipate()
        {
            if (!_isActive) return;

            _isActive = false;
            _remainingDuration = 0f;
            LOSBlockerRegistry.Unregister(this);
            ReleaseAllSuppressedSentinels();

            NullLog.Info("NullCloudSmoke", "Null-Cloud Smoke dissipated.");
            OnSmokeDissipated?.Invoke(this);

            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
                _lifetimeCoroutine = null;
            }

            gameObject.SetActive(false);
        }

        private void EvaluateSuppressionSphere()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _fieldRadius, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < colliders.Length; i++)
            {
                var reaction = colliders[i].GetComponentInParent<ISentinelSmokeReaction>();
                if (reaction != null && !_suppressedSentinels.Contains(reaction))
                {
                    _suppressedSentinels.Add(reaction);
                    reaction.OnSmokeSuppressed(transform.position, _fieldRadius);
                    NullLog.Info("NullCloudSmoke", $"Sentinel {colliders[i].name} caught in Null-Cloud Smoke! Ceasing fire and seeking cover (FR-19).");
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;

            var reaction = other.GetComponentInParent<ISentinelSmokeReaction>();
            if (reaction != null && !_suppressedSentinels.Contains(reaction))
            {
                _suppressedSentinels.Add(reaction);
                reaction.OnSmokeSuppressed(transform.position, _fieldRadius);
                NullLog.Info("NullCloudSmoke", $"Sentinel {other.name} entered Null-Cloud Smoke! Suppressed.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_isActive) return;

            var reaction = other.GetComponentInParent<ISentinelSmokeReaction>();
            if (reaction != null && _suppressedSentinels.Contains(reaction))
            {
                _suppressedSentinels.Remove(reaction);
                reaction.OnSmokeCleared();
                NullLog.Info("NullCloudSmoke", $"Sentinel {other.name} exited Null-Cloud Smoke.");
            }
        }

        /// <summary>
        /// Evaluates whether a line segment from 'from' to 'to' passes through the static cloud (FR-19).
        /// </summary>
        public bool BlocksLineOfSight(Vector3 from, Vector3 to)
        {
            if (!_isActive) return false;

            // Distance from point (cloud center) to line segment (from -> to)
            Vector3 center = transform.position;
            Vector3 segment = to - from;
            float segmentLength = segment.magnitude;
            if (segmentLength < 0.001f)
            {
                return (from - center).sqrMagnitude <= (_fieldRadius * _fieldRadius);
            }

            Vector3 segDir = segment / segmentLength;
            float t = Vector3.Dot(center - from, segDir);
            t = Mathf.Clamp(t, 0f, segmentLength);

            Vector3 closestPoint = from + segDir * t;
            return (closestPoint - center).sqrMagnitude <= (_fieldRadius * _fieldRadius);
        }

        public bool IsPointInside(Vector3 point)
        {
            if (!_isActive) return false;
            return (point - transform.position).sqrMagnitude <= (_fieldRadius * _fieldRadius);
        }

        private void ReleaseAllSuppressedSentinels()
        {
            foreach (var sentinel in _suppressedSentinels)
            {
                sentinel?.OnSmokeCleared();
            }
            _suppressedSentinels.Clear();
        }

        public void ResetState()
        {
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
                _lifetimeCoroutine = null;
            }

            _isActive = false;
            _remainingDuration = 0f;
            LOSBlockerRegistry.Unregister(this);
            ReleaseAllSuppressedSentinels();
            gameObject.SetActive(false);
        }
    }
}
