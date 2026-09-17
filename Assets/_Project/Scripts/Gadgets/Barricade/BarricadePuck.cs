using System;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Gadgets
{
    /// <summary>
    /// Thrown puck projectile that deploys a Hard-Light Barricade upon landing (FR-16).
    /// </summary>
    public class BarricadePuck : MonoBehaviour
    {
        [Header("Toss & Physics")]
        [SerializeField] private float _throwSpeed = 14f;
        [SerializeField] private float _gravityMultiplier = 1.2f;
        [SerializeField] private LayerMask _groundMask = ~0;

        [Header("Prefab Reference")]
        [SerializeField] private DeployableBarricade _barricadePrefab;

        private Vector3 _velocity;
        private bool _hasLanded;
        private Vector3 _throwerForward;

        public event Action<Vector3, Quaternion> OnPuckLanded;

        public float ThrowSpeed => _throwSpeed;
        public bool HasLanded => _hasLanded;

        public void Launch(Vector3 origin, Vector3 direction, DeployableBarricade barricadeInstance = null)
        {
            transform.position = origin;
            _throwerForward = direction.normalized;
            _velocity = (_throwerForward + Vector3.up * 0.15f).normalized * _throwSpeed;
            _hasLanded = false;
            gameObject.SetActive(true);

            if (barricadeInstance != null)
            {
                _barricadePrefab = barricadeInstance;
            }
        }

        private void Update()
        {
            if (_hasLanded) return;

            // Apply gravity
            _velocity += Physics.gravity * (_gravityMultiplier * Time.deltaTime);

            Vector3 step = _velocity * Time.deltaTime;
            float stepDistance = step.magnitude;

            if (Physics.Raycast(transform.position, _velocity.normalized, out RaycastHit hit, stepDistance, _groundMask, QueryTriggerInteraction.Ignore))
            {
                Land(hit.point, hit.normal);
            }
            else
            {
                transform.position += step;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasLanded) return;
            ContactPoint contact = collision.GetContact(0);
            Land(contact.point, contact.normal);
        }

        public void Land(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (_hasLanded) return;
            _hasLanded = true;

            // Align rotation so barricade faces perpendicular to throw direction or along ground
            Vector3 forward = Vector3.ProjectOnPlane(_throwerForward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            Quaternion deployRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

            NullLog.Info("BarricadePuck", $"Puck landed at {hitPoint}. Initiating barricade expansion (FR-16).");
            OnPuckLanded?.Invoke(hitPoint, deployRotation);

            if (_barricadePrefab != null)
            {
                _barricadePrefab.Deploy(hitPoint, deployRotation);
            }

            gameObject.SetActive(false);
        }
    }
}
