using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
using NullProtocol.Core;

namespace NullProtocol.Gadgets
{
    /// <summary>
    /// Hard-Light Barricade deployable cover (FR-16, FR-17).
    /// Deploys a 1.8m x 1.1m physical barrier with 400 HP within 0.8s of puck landing.
    /// Dynamically carves a runtime NavMeshObstacle within 5 ms to prevent AI pathing.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DeployableBarricade : MonoBehaviour, IDamageable, IResettable
    {
        [Header("Dimensions (FR-16)")]
        [Tooltip("Width of barrier in meters (1.8m)")]
        [SerializeField] private float _barrierWidth = 1.8f;
        [Tooltip("Height of barrier in meters (1.1m)")]
        [SerializeField] private float _barrierHeight = 1.1f;
        [Tooltip("Depth / thickness of barrier in meters (0.2m)")]
        [SerializeField] private float _barrierThickness = 0.2f;

        [Header("Health & Timing (FR-16, FR-17)")]
        [Tooltip("Maximum health pool of barrier (400 HP)")]
        [SerializeField] private int _maxHealth = 400;
        [Tooltip("Time from puck landing until barrier is fully expanded (0.8s)")]
        [SerializeField] private float _deploymentTime = 0.8f;

        [Header("Components")]
        [SerializeField] private NavMeshObstacle _navObstacle;
        [SerializeField] private BoxCollider _boxCollider;

        // Runtime State
        private int _currentHealth;
        private bool _isDeployed;
        private bool _isDeploying;
        private bool _isDestroyed;
        private float _deploymentProgress;
        private double _lastCarveDurationMs;
        private Coroutine _deployCoroutine;

        public event Action<DeployableBarricade> OnBarricadeDeployed;
        public event Action<DeployableBarricade> OnBarricadeDestroyed;

        public float BarrierWidth => _barrierWidth;
        public float BarrierHeight => _barrierHeight;
        public float BarrierThickness => _barrierThickness;
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public float DeploymentTime => _deploymentTime;
        public bool IsDeployed => _isDeployed;
        public bool IsDeploying => _isDeploying;
        public bool IsDestroyed => _isDestroyed;
        public float DeploymentProgress => _deploymentProgress;
        public double LastCarveDurationMs => _lastCarveDurationMs;
        public NavMeshObstacle NavObstacle => _navObstacle;
        public BoxCollider BarrierCollider => _boxCollider;

        private void Awake()
        {
            if (_boxCollider == null)
            {
                _boxCollider = GetComponent<BoxCollider>();
            }

            if (_navObstacle == null)
            {
                _navObstacle = GetComponent<NavMeshObstacle>();
                if (_navObstacle == null)
                {
                    _navObstacle = gameObject.AddComponent<NavMeshObstacle>();
                }
            }

            ConfigureComponents();
            _currentHealth = _maxHealth;
            gameObject.tag = Tags.DeployableBarricade;
        }

        private void ConfigureComponents()
        {
            Vector3 size = new Vector3(_barrierWidth, _barrierHeight, _barrierThickness);
            Vector3 center = new Vector3(0f, _barrierHeight * 0.5f, 0f);

            if (_boxCollider != null)
            {
                _boxCollider.size = size;
                _boxCollider.center = center;
            }

            if (_navObstacle != null)
            {
                _navObstacle.shape = NavMeshObstacleShape.Box;
                _navObstacle.size = size;
                _navObstacle.center = center;
                _navObstacle.carving = true;
                _navObstacle.carveOnlyStationary = true;
                _navObstacle.enabled = false;
            }
        }

        /// <summary>
        /// Deploys the barrier at the specified position and orientation (FR-16).
        /// Takes 0.8s to fully expand, then carves NavMeshObstacle within 5 ms (FR-17).
        /// </summary>
        public void Deploy(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _currentHealth = _maxHealth;
            _isDestroyed = false;
            _isDeployed = false;
            _isDeploying = true;
            _deploymentProgress = 0f;

            gameObject.SetActive(true);

            if (_deployCoroutine != null)
            {
                StopCoroutine(_deployCoroutine);
            }
            _deployCoroutine = StartCoroutine(DeployRoutine());
        }

        /// <summary>
        /// Immediate deployment for testing or scripted scenarios.
        /// </summary>
        public void DeployImmediate(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _currentHealth = _maxHealth;
            _isDestroyed = false;
            _isDeploying = false;
            _isDeployed = true;
            _deploymentProgress = 1f;

            gameObject.SetActive(true);
            ActivateNavMeshObstacle();
            OnBarricadeDeployed?.Invoke(this);
        }

        private IEnumerator DeployRoutine()
        {
            float elapsed = 0f;
            Vector3 targetScale = Vector3.one;

            // Start flat or small
            transform.localScale = new Vector3(targetScale.x, 0.05f, targetScale.z);

            while (elapsed < _deploymentTime)
            {
                elapsed += Time.deltaTime;
                _deploymentProgress = Mathf.Clamp01(elapsed / _deploymentTime);

                // Smooth expansion
                float currentY = Mathf.Lerp(0.05f, targetScale.y, _deploymentProgress);
                transform.localScale = new Vector3(targetScale.x, currentY, targetScale.z);

                yield return null;
            }

            transform.localScale = targetScale;
            _deploymentProgress = 1f;
            _isDeploying = false;
            _isDeployed = true;

            // Carve NavMesh dynamically within 5ms (FR-17)
            ActivateNavMeshObstacle();

            NullLog.Info("Barricade", $"Hard-Light Barricade deployed in {_deploymentTime:F1}s! Carve time: {_lastCarveDurationMs:F3}ms");
            OnBarricadeDeployed?.Invoke(this);
            _deployCoroutine = null;
        }

        private void ActivateNavMeshObstacle()
        {
            if (_navObstacle != null)
            {
                var stopwatch = Stopwatch.StartNew();
                _navObstacle.enabled = true;
                stopwatch.Stop();
                _lastCarveDurationMs = stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot)
        {
            if (_isDestroyed || !_isDeployed) return;

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            NullLog.Info("Barricade", $"Hard-Light Barricade absorbed {amount} damage. HP: {_currentHealth}/{_maxHealth}");

            if (_currentHealth <= 0)
            {
                Shatter();
            }
        }

        private void Shatter()
        {
            _isDestroyed = true;
            _isDeployed = false;
            _isDeploying = false;

            if (_navObstacle != null)
            {
                _navObstacle.enabled = false;
            }

            NullLog.Info("Barricade", "Hard-Light Barricade shattered!");
            OnBarricadeDestroyed?.Invoke(this);

            if (_deployCoroutine != null)
            {
                StopCoroutine(_deployCoroutine);
                _deployCoroutine = null;
            }

            gameObject.SetActive(false);
        }

        public void ResetState()
        {
            if (_deployCoroutine != null)
            {
                StopCoroutine(_deployCoroutine);
                _deployCoroutine = null;
            }

            _currentHealth = _maxHealth;
            _isDeployed = false;
            _isDeploying = false;
            _isDestroyed = false;
            _deploymentProgress = 0f;

            if (_navObstacle != null)
            {
                _navObstacle.enabled = false;
            }

            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }
    }
}
