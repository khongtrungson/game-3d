using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Controller
{
    [DisallowMultipleComponent]
    public class CameraLeanController : MonoBehaviour
    {
        [Header("Lean Configuration (FR-5)")]
        [Tooltip("Maximum camera roll angle in degrees (FR-5: 18°)")]
        [SerializeField] private float _maxLeanAngle = 18.0f;

        [Tooltip("Maximum lateral offset in meters (FR-5: 0.35m)")]
        [SerializeField] private float _maxLeanOffset = 0.35f;

        [Tooltip("Transition duration in seconds (FR-5: 0.15s)")]
        [SerializeField] private float _leanTransitionDuration = 0.15f;

        [Header("Clipping Prevention (FR-6)")]
        [Tooltip("Radius of continuous sphere-cast to prevent wall penetration")]
        [SerializeField] private float _sphereCastRadius = 0.15f;

        [Tooltip("Collision layers that block lean displacement")]
        [SerializeField] private LayerMask _obstacleMask = ~0;

        [Header("Event Channels")]
        [SerializeField] private PlayerStateEventChannelSO _playerStateEvents;

        [Header("Debug")]
        [SerializeField] private bool _drawDebugGizmos = true;

        private float _targetLeanInput; // -1 (Left/Q), 0, 1 (Right/E)
        private float _currentLeanFactor; // smoothly interpolated between -1 and 1
        private float _leanVelocity; // for SmoothDamp

        private Vector3 _neutralLocalPosition;
        private float _currentRollAngle;
        private Vector3 _currentCameraOffset;

        public float MaxLeanAngle => _maxLeanAngle;
        public float MaxLeanOffset => _maxLeanOffset;
        public float LeanTransitionDuration => _leanTransitionDuration;
        public float CurrentRollAngle => _currentRollAngle;
        public float CurrentLeanFactor => _currentLeanFactor;
        public Vector3 CurrentCameraOffset => _currentCameraOffset;

        private void Awake()
        {
            _neutralLocalPosition = transform.localPosition;
            if (_obstacleMask == ~0)
            {
                // Default to ignoring triggers & non-colliders
                _obstacleMask = Layers.CameraObstacles;
            }
        }

        public void SetNeutralPosition(Vector3 neutralPos)
        {
            _neutralLocalPosition = neutralPos;
        }

        public void UpdateLeanInput(float leanInput)
        {
            _targetLeanInput = Mathf.Clamp(leanInput, -1f, 1f);
        }

        public void TickLean(float deltaTime)
        {
            // Smoothly interpolate lean factor over _leanTransitionDuration
            _currentLeanFactor = Mathf.SmoothDamp(
                _currentLeanFactor,
                _targetLeanInput,
                ref _leanVelocity,
                _leanTransitionDuration,
                float.PositiveInfinity,
                deltaTime
            );

            // Compute desired roll angle: leaning right (input > 0) rolls negative z or positive depending on coordinate convention.
            // In standard FPS: leaning right rolls camera clockwise (-18 deg z)
            _currentRollAngle = -_currentLeanFactor * _maxLeanAngle;

            // Compute desired lateral displacement
            float desiredDisplacement = _currentLeanFactor * _maxLeanOffset;

            // Perform continuous sphere-cast clipping check (FR-6)
            float allowedDisplacement = CheckClipping(desiredDisplacement);

            _currentCameraOffset = Vector3.right * allowedDisplacement;
            transform.localPosition = _neutralLocalPosition + _currentCameraOffset;

            _playerStateEvents?.RaiseLeanChanged(_currentLeanFactor);
        }

        /// <summary>
        /// Performs continuous sphere-cast from camera origin laterally to prevent camera clipping into adjacent walls.
        /// </summary>
        private float CheckClipping(float desiredDisplacement)
        {
            if (Mathf.Abs(desiredDisplacement) < 0.001f)
            {
                return 0f;
            }

            Vector3 worldOrigin = transform.parent != null 
                ? transform.parent.TransformPoint(_neutralLocalPosition) 
                : transform.position;

            Vector3 worldDirection = (transform.parent != null ? transform.parent.right : transform.right) * Mathf.Sign(desiredDisplacement);
            float castDistance = Mathf.Abs(desiredDisplacement);

            if (Physics.SphereCast(worldOrigin, _sphereCastRadius, worldDirection, out RaycastHit hit, castDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // Wall detected: truncate lean displacement to hit distance minus a skin buffer
                float safeDistance = Mathf.Max(0f, hit.distance - 0.02f);
                return safeDistance * Mathf.Sign(desiredDisplacement);
            }

            return desiredDisplacement;
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawDebugGizmos) return;

            Vector3 worldOrigin = transform.parent != null 
                ? transform.parent.TransformPoint(_neutralLocalPosition) 
                : transform.position;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(worldOrigin, _sphereCastRadius);

            Vector3 rightDir = transform.parent != null ? transform.parent.right : transform.right;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(worldOrigin, worldOrigin + rightDir * _maxLeanOffset);
            Gizmos.DrawLine(worldOrigin, worldOrigin - rightDir * _maxLeanOffset);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _sphereCastRadius);
        }
    }
}
