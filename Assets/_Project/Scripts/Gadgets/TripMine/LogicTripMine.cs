using System;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Gadgets
{
    /// <summary>
    /// Logic-Trip Mine fortification gadget (FR-20, FR-21).
    /// Mounts to doorframe/wall surfaces and emits a 3.0m horizontal laser tripwire.
    /// Triggers when hostiles cross the beam, freezing them into an immobile wireframe state for 5.0s.
    /// </summary>
    public class LogicTripMine : MonoBehaviour, IResettable
    {
        [Header("Laser Tripwire Settings (FR-20)")]
        [Tooltip("Maximum laser tripwire length in meters (3.0m)")]
        [SerializeField] private float _laserLength = 3.0f;
        [Tooltip("Detection radius around the laser beam in meters")]
        [SerializeField] private float _beamDetectionRadius = 0.15f;
        [Tooltip("Layers checked for beam crossing and obstacle clipping")]
        [SerializeField] private LayerMask _tripwireMask = ~0;

        [Header("Freeze Settings (FR-21)")]
        [Tooltip("Duration of wireframe freeze in seconds (5.0s)")]
        [SerializeField] private float _freezeDuration = 5.0f;

        [Header("Components")]
        [SerializeField] private LineRenderer _lineRenderer;

        // Runtime State
        private bool _isArmed;
        private bool _isTriggered;
        private Vector3 _beamDirection = Vector3.forward;
        private float _currentBeamLength;
        private Vector3 _mountNormal = Vector3.forward;

        public event Action<LogicTripMine, GameObject> OnMineTriggered;

        public float LaserLength => _laserLength;
        public float CurrentBeamLength => _currentBeamLength;
        public float FreezeDuration => _freezeDuration;
        public bool IsArmed => _isArmed;
        public bool IsTriggered => _isTriggered;
        public Vector3 BeamDirection => _beamDirection;
        public Vector3 MountNormal => _mountNormal;

        private void Awake()
        {
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
            }

            _currentBeamLength = _laserLength;
        }

        /// <summary>
        /// Mounts the mine to a doorframe or wall surface (FR-20).
        /// Emits a 3.0m horizontal laser tripwire perpendicular to surface normal or along given direction.
        /// </summary>
        public void Mount(Vector3 position, Vector3 surfaceNormal, Vector3? preferredLaserDirection = null)
        {
            transform.position = position;
            _mountNormal = surfaceNormal.normalized;

            // Compute horizontal laser direction across the doorframe/opening
            if (preferredLaserDirection.HasValue && preferredLaserDirection.Value.sqrMagnitude > 0.001f)
            {
                _beamDirection = preferredLaserDirection.Value.normalized;
            }
            else
            {
                // Default: horizontal across surface (tangent to normal on horizontal plane)
                Vector3 horizontalTangent = Vector3.Cross(_mountNormal, Vector3.up).normalized;
                if (horizontalTangent.sqrMagnitude < 0.001f)
                {
                    horizontalTangent = Vector3.Cross(_mountNormal, Vector3.forward).normalized;
                }
                _beamDirection = horizontalTangent;
            }

            transform.rotation = Quaternion.LookRotation(_mountNormal, Vector3.up);

            _isArmed = true;
            _isTriggered = false;
            gameObject.SetActive(true);

            UpdateLaserBeam();
            NullLog.Info("LogicTripMine", $"Logic-Trip Mine mounted at {position} emitting {_laserLength:F1}m laser tripwire (FR-20).");
        }

        private void Update()
        {
            if (!_isArmed || _isTriggered) return;

            UpdateLaserBeam();
            CheckBeamCrossing();
        }

        private void UpdateLaserBeam()
        {
            Vector3 origin = transform.position;
            _currentBeamLength = _laserLength;

            // Clip beam against geometry/walls closer than 3.0m
            if (Physics.Raycast(origin, _beamDirection, out RaycastHit hit, _laserLength, Layers.Wall, QueryTriggerInteraction.Ignore))
            {
                _currentBeamLength = hit.distance;
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = true;
                _lineRenderer.positionCount = 2;
                _lineRenderer.SetPosition(0, origin);
                _lineRenderer.SetPosition(1, origin + _beamDirection * _currentBeamLength);
            }
        }

        public void CheckBeamCrossing()
        {
            if (!_isArmed || _isTriggered) return;

            Vector3 origin = transform.position;
            Ray ray = new Ray(origin, _beamDirection);

            // SphereCast along the 3.0m laser tripwire to detect crossing hostiles
            RaycastHit[] hits = Physics.SphereCastAll(ray, _beamDetectionRadius, _currentBeamLength, _tripwireMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCol = hits[i].collider;
                if (hitCol.gameObject == gameObject) continue;

                // Check if this collider belongs to an enemy/hostile
                if (IsHostile(hitCol))
                {
                    TriggerMine(hitCol.gameObject);
                    break;
                }
            }
        }

        private bool IsHostile(Collider col)
        {
            if (col.CompareTag(Tags.Enemy) || col.CompareTag(Tags.EnemyHead)) return true;

            // Check if object has IFreezableHostile or IDamageable attached (and not player)
            if (col.CompareTag(Tags.Player)) return false;

            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null && !col.transform.root.CompareTag(Tags.Player)) return true;

            string nameLower = col.gameObject.name.ToLowerInvariant();
            return nameLower.Contains("sentinel") || nameLower.Contains("enemy") || nameLower.Contains("hostile");
        }

        /// <summary>
        /// Detonates the mine when crossed, freezing the target for 5.0s (FR-21).
        /// </summary>
        public void TriggerMine(GameObject target)
        {
            if (_isTriggered) return;

            _isTriggered = true;
            _isArmed = false;

            NullLog.Info("LogicTripMine", $"Laser tripwire tripped by {target.name}! Triggering 5.0s freeze (FR-21).");

            // Freeze the crossing hostile (FR-21)
            var freezable = target.GetComponentInParent<IFreezableHostile>();
            if (freezable != null)
            {
                freezable.ApplyWireframeFreeze(_freezeDuration);
            }
            else
            {
                // Ensure a FrozenHostileDebuff component is attached
                var debuff = target.GetComponentInParent<FrozenHostileDebuff>() ?? target.AddComponent<FrozenHostileDebuff>();
                debuff.ApplyWireframeFreeze(_freezeDuration);
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }

            OnMineTriggered?.Invoke(this, target);
        }

        public void ResetState()
        {
            _isArmed = false;
            _isTriggered = false;
            _currentBeamLength = _laserLength;

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }

            gameObject.SetActive(false);
        }
    }
}
