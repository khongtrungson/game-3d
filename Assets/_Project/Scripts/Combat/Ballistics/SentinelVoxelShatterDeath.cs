using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Combat
{
    /// <summary>
    /// FR-38: Eliminated sentinels shall freeze for 0.15s before shattering into 30–50 physics-driven
    /// wireframe voxels that dissolve over 2.0s.
    /// </summary>
    [DisallowMultipleComponent]
    public class SentinelVoxelShatterDeath : MonoBehaviour, IResettable
    {
        [Header("Timing (FR-38)")]
        [Tooltip("Pre-shatter freeze delay in seconds (0.15s per FR-38)")]
        [SerializeField] private float _freezeDuration = 0.15f;

        [Tooltip("Voxel dissolution duration in seconds (2.0s per FR-38)")]
        [SerializeField] private float _dissolveDuration = 2.0f;

        [Header("Voxel Count & Scattering (FR-38: 30-50 voxels)")]
        [Range(30, 50)]
        [SerializeField] private int _voxelCount = 40;
        [SerializeField] private float _voxelSize = 0.18f;
        [SerializeField] private float _explosionForce = 6.0f;
        [SerializeField] private float _scatterRadius = 1.2f;

        [Header("Visuals & Materials")]
        [SerializeField] private Material _wireframeVoxelMaterial;
        [ColorUsage(true, true)]
        [SerializeField] private Color _emissiveWireColor = new Color(0f, 0.9f, 1f, 1f) * 2.0f;

        [Header("Sentinel References")]
        [SerializeField] private SentinelHealth _health;
        [SerializeField] private Renderer[] _sentinelRenderers;
        [SerializeField] private Collider[] _sentinelColliders;

        private readonly List<GameObject> _spawnedVoxels = new List<GameObject>();
        private Coroutine _deathRoutine;
        private bool _isShattered;

        public float FreezeDuration => _freezeDuration;
        public float DissolveDuration => _dissolveDuration;
        public int VoxelCount => _voxelCount;
        public bool IsShattered => _isShattered;
        public IReadOnlyList<GameObject> SpawnedVoxels => _spawnedVoxels;

        private void Awake()
        {
            if (_health == null) _health = GetComponent<SentinelHealth>();
            if (_sentinelRenderers == null || _sentinelRenderers.Length == 0)
            {
                _sentinelRenderers = GetComponentsInChildren<Renderer>();
            }
            if (_sentinelColliders == null || _sentinelColliders.Length == 0)
            {
                _sentinelColliders = GetComponentsInChildren<Collider>();
            }
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                // Can hook to health death if health exposes or via damage checking
            }
        }

        /// <summary>
        /// Initiates the FR-38 death sequence: freeze for 0.15s, then shatter into 30-50 voxels dissolving over 2.0s.
        /// </summary>
        public void TriggerShatterDeath(Vector3 impactPoint, Vector3 impactNormal)
        {
            if (_isShattered) return;
            _isShattered = true;

            if (_deathRoutine != null) StopCoroutine(_deathRoutine);
            _deathRoutine = StartCoroutine(ShatterSequenceRoutine(impactPoint, impactNormal));
        }

        private IEnumerator ShatterSequenceRoutine(Vector3 impactPoint, Vector3 impactNormal)
        {
            // 1. Freeze sentinel for 0.15s (FR-38)
            NullLog.Info("SentinelVoxel", $"Sentinel freezing for {_freezeDuration:F2}s before voxel shatter...");
            yield return new WaitForSeconds(_freezeDuration);

            // 2. Hide sentinel original meshes and disable colliders
            SetSentinelVisibility(false);

            // 3. Spawn 30–50 physics-driven wireframe voxels
            SpawnVoxels(impactPoint, impactNormal);

            // 4. Dissolve over 2.0s (FR-38)
            yield return StartCoroutine(DissolveVoxelsRoutine(_dissolveDuration));

            // 5. Cleanup voxels
            CleanupVoxels();
        }

        private void SpawnVoxels(Vector3 centerPoint, Vector3 impactNormal)
        {
            CleanupVoxels();

            int count = Mathf.Clamp(_voxelCount, 30, 50);
            Vector3 origin = (centerPoint != Vector3.zero) ? centerPoint : transform.position + Vector3.up * 0.9f;

            for (int i = 0; i < count; i++)
            {
                GameObject voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                voxel.name = $"WireframeVoxel_{i}";
                voxel.transform.localScale = Vector3.one * _voxelSize;

                // Random distribution within sentinel bounding volume
                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.4f, 0.4f),
                    Random.Range(-0.8f, 0.8f),
                    Random.Range(-0.4f, 0.4f)
                );
                voxel.transform.position = origin + randomOffset;
                voxel.transform.rotation = Random.rotation;

                // Physics - SỬA LỖI TẠI ĐÂY: Dùng kiểm tra if (rb == null) chuẩn của Unity thay vì toán tử ??
                var rb = voxel.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = voxel.AddComponent<Rigidbody>();
                }

                rb.mass = 0.2f;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

                // Scatter force away from center + impact normal
                Vector3 blastDir = (randomOffset.normalized + impactNormal * 0.5f + Vector3.up * 0.4f).normalized;
                rb.AddForce(blastDir * Random.Range(_explosionForce * 0.6f, _explosionForce * 1.4f), ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);

                // Material styling
                var renderer = voxel.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (_wireframeVoxelMaterial != null)
                    {
                        renderer.material = _wireframeVoxelMaterial;
                    }
                    else
                    {
                        var shader = Shader.Find("NullProtocol/FlatWireframeEdge") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                        if (shader != null)
                        {
                            var mat = new Material(shader);
                            mat.color = _emissiveWireColor;
                            renderer.material = mat;
                        }
                    }
                }

                _spawnedVoxels.Add(voxel);
            }

            NullLog.Info("SentinelVoxel", $"Shattered sentinel into {_spawnedVoxels.Count} physics wireframe voxels (FR-38).");
        }

        private IEnumerator DissolveVoxelsRoutine(float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * _voxelSize;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scaleMultiplier = Mathf.Lerp(1.0f, 0.0f, progress);

                for (int i = 0; i < _spawnedVoxels.Count; i++)
                {
                    var v = _spawnedVoxels[i];
                    if (v != null)
                    {
                        v.transform.localScale = startScale * scaleMultiplier;
                    }
                }

                yield return null;
            }
        }

        private void SetSentinelVisibility(bool visible)
        {
            if (_sentinelRenderers != null)
            {
                for (int i = 0; i < _sentinelRenderers.Length; i++)
                {
                    if (_sentinelRenderers[i] != null)
                    {
                        _sentinelRenderers[i].enabled = visible;
                    }
                }
            }

            if (_sentinelColliders != null)
            {
                for (int i = 0; i < _sentinelColliders.Length; i++)
                {
                    if (_sentinelColliders[i] != null)
                    {
                        _sentinelColliders[i].enabled = visible;
                    }
                }
            }
        }

        private void CleanupVoxels()
        {
            for (int i = 0; i < _spawnedVoxels.Count; i++)
            {
                if (_spawnedVoxels[i] != null)
                {
                    Destroy(_spawnedVoxels[i]);
                }
            }
            _spawnedVoxels.Clear();
        }

        public void ResetState()
        {
            if (_deathRoutine != null)
            {
                StopCoroutine(_deathRoutine);
                _deathRoutine = null;
            }

            CleanupVoxels();
            SetSentinelVisibility(true);
            _isShattered = false;
        }

        private void OnDestroy()
        {
            CleanupVoxels();
        }
    }
}