using System;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Gadgets
{
    /// <summary>
    /// Thrown canister projectile that detonates on impact into Null-Cloud Smoke (FR-18).
    /// </summary>
    public class SmokeCanister : MonoBehaviour
    {
        [Header("Toss & Physics")]
        [SerializeField] private float _throwSpeed = 16f;
        [SerializeField] private float _gravityMultiplier = 1.0f;
        [SerializeField] private LayerMask _impactMask = ~0;

        [Header("Smoke Reference")]
        [SerializeField] private NullCloudSmoke _smokePrefab;

        private Vector3 _velocity;
        private bool _hasDetonated;

        public event Action<Vector3> OnCanisterDetonated;

        public float ThrowSpeed => _throwSpeed;
        public bool HasDetonated => _hasDetonated;

        public void Launch(Vector3 origin, Vector3 direction, NullCloudSmoke smokeInstance = null)
        {
            transform.position = origin;
            Vector3 throwDir = (direction.normalized + Vector3.up * 0.12f).normalized;
            _velocity = throwDir * _throwSpeed;
            _hasDetonated = false;
            gameObject.SetActive(true);

            if (smokeInstance != null)
            {
                _smokePrefab = smokeInstance;
            }
        }

        private void Update()
        {
            if (_hasDetonated) return;

            _velocity += Physics.gravity * (_gravityMultiplier * Time.deltaTime);
            Vector3 step = _velocity * Time.deltaTime;
            float stepDistance = step.magnitude;

            if (Physics.Raycast(transform.position, _velocity.normalized, out RaycastHit hit, stepDistance, _impactMask, QueryTriggerInteraction.Ignore))
            {
                DetonateOnImpact(hit.point);
            }
            else
            {
                transform.position += step;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasDetonated) return;
            ContactPoint contact = collision.GetContact(0);
            DetonateOnImpact(contact.point);
        }

        public void DetonateOnImpact(Vector3 impactPoint)
        {
            if (_hasDetonated) return;
            _hasDetonated = true;

            NullLog.Info("SmokeCanister", $"Smoke canister detonated on impact at {impactPoint} (FR-18).");
            OnCanisterDetonated?.Invoke(impactPoint);

            if (_smokePrefab != null)
            {
                _smokePrefab.Detonate(impactPoint);
            }

            gameObject.SetActive(false);
        }
    }
}
