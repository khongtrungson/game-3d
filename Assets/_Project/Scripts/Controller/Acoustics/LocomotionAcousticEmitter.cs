using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Controller
{
    [DisallowMultipleComponent]
    public class LocomotionAcousticEmitter : MonoBehaviour
    {
        [Header("Stimulus Radii")]
        [Tooltip("Crouch acoustic radius in meters (FR-7: 1.5m)")]
        [SerializeField] private float _crouchRadius = 1.5f;

        [Tooltip("Walk acoustic radius in meters (FR-7: 6.0m)")]
        [SerializeField] private float _walkRadius = 6.0f;

        [Tooltip("Sprint acoustic radius in meters (FR-7: 18.0m)")]
        [SerializeField] private float _sprintRadius = 18.0f;

        [Header("Step Interval Settings")]
        [SerializeField] private float _walkStepDistance = 1.8f;
        [SerializeField] private float _crouchStepDistance = 1.4f;
        [SerializeField] private float _sprintStepDistance = 2.2f;

        [Header("Event Channel")]
        [SerializeField] private AcousticStimulusEventChannelSO _acousticEvents;

        [Header("Debug")]
        [SerializeField] private bool _drawDebugGizmos = true;

        private float _distanceCovered;
        private Vector3 _lastPosition;
        private float _lastEmittedRadius;
        private float _lastEmitTime;

        public float CrouchRadius => _crouchRadius;
        public float WalkRadius => _walkRadius;
        public float SprintRadius => _sprintRadius;

        private void Start()
        {
            _lastPosition = transform.position;
        }

        public void ProcessMovement(Vector3 currentPosition, bool isGrounded, bool isMoving, bool isCrouching, bool isSprinting)
        {
            if (!isGrounded || !isMoving)
            {
                _lastPosition = currentPosition;
                return;
            }

            Vector3 delta = currentPosition - _lastPosition;
            delta.y = 0f; // Horizontal distance only
            _distanceCovered += delta.magnitude;
            _lastPosition = currentPosition;

            float stepInterval = isSprinting ? _sprintStepDistance : (isCrouching ? _crouchStepDistance : _walkStepDistance);

            if (_distanceCovered >= stepInterval)
            {
                _distanceCovered = 0f;
                EmitFootstep(currentPosition, isCrouching, isSprinting);
            }
        }

        public void EmitFootstep(Vector3 position, bool isCrouching, bool isSprinting)
        {
            float radius = _walkRadius;
            AcousticStimulusType type = AcousticStimulusType.WalkFootstep;

            if (isCrouching)
            {
                radius = _crouchRadius;
                type = AcousticStimulusType.CrouchFootstep;
            }
            else if (isSprinting)
            {
                radius = _sprintRadius;
                type = AcousticStimulusType.SprintFootstep;
            }

            _lastEmittedRadius = radius;
            _lastEmitTime = Time.time;

            _acousticEvents?.RaiseStimulus(position, radius, type);
            NullLog.Info("Acoustics", $"Footstep emitted at {position}, radius: {radius}m, type: {type}");
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawDebugGizmos) return;

            // Draw current active sound wave if recently emitted (fade over 0.5s)
            float timeSinceEmit = Time.time - _lastEmitTime;
            if (timeSinceEmit < 0.5f && _lastEmittedRadius > 0f)
            {
                Gizmos.color = new Color(0f, 0.8f, 1f, 1f - (timeSinceEmit / 0.5f));
                Gizmos.DrawWireSphere(transform.position, _lastEmittedRadius);
            }
        }
    }
}
