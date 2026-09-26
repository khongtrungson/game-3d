using System.Collections;
using UnityEngine;

namespace NullProtocol.VFX
{
    /// <summary>
    /// Spawns and drives the pre-fractured de-rez voxel burst upon hostile elimination.
    /// Specifications:
    /// - 0.15s digitization freeze upon fatal hit.
    /// - Violent radial shatter into 30-50 low-poly physics voxels (CHR-VOXEL-DEBRIS).
    /// - 2.0s Voronoi noise alpha cutoff dissolve with intense glowing cyan wireframe border.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyDeRezSpawner : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Brief freeze duration simulating digitalization lag upon hostile death.")]
        [SerializeField] private float freezeDuration = 0.15f;

        [Tooltip("Total duration over which voxel shards dissolve into the digital void.")]
        [SerializeField] private float dissolveDuration = 2.0f;

        [Header("Physics Burst")]
        [SerializeField] private float burstForceMin = 4.5f;
        [SerializeField] private float burstForceMax = 8.5f;
        [SerializeField] private float upwardLift = 2.0f;
        [SerializeField] private float torqueStrength = 18.0f;

        [Header("Components")]
        [SerializeField] private ParticleSystem shockwaveParticles;
        [SerializeField] private ParticleSystem flashParticles;
        [SerializeField] private ParticleSystem sparkParticles;
        [SerializeField] private Light burstLight;
        [SerializeField] private AudioSource sfxSource;

        private VoxelDissolvePhysics[] childVoxels;
        private static readonly int DissolveAmountProp = Shader.PropertyToID("_DissolveAmount");

        private void Awake()
        {
            childVoxels = GetComponentsInChildren<VoxelDissolvePhysics>(true);
        }

        private void Start()
        {
            StartCoroutine(RoutineDeRezBurst());
        }

        private IEnumerator RoutineDeRezBurst()
        {
            // 1. Initial 0.15s digitization freeze
            if (burstLight != null)
            {
                burstLight.intensity = 1.5f;
                burstLight.enabled = true;
            }

            yield return new WaitForSeconds(freezeDuration);

            // 2. Violent Burst Shatter
            if (shockwaveParticles != null) shockwaveParticles.Play();
            if (flashParticles != null) flashParticles.Play();
            if (sparkParticles != null) sparkParticles.Play();
            if (sfxSource != null) sfxSource.Play();

            if (burstLight != null)
            {
                burstLight.intensity = 5.0f;
            }

            Vector3 burstOrigin = transform.position + Vector3.up * 0.9f;

            // Activate and launch physics voxels
            if (childVoxels != null)
            {
                foreach (var voxel in childVoxels)
                {
                    if (voxel == null) continue;
                    voxel.gameObject.SetActive(true);

                    Rigidbody rb = voxel.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        Vector3 dir = (voxel.transform.position - burstOrigin).normalized;
                        dir += Random.insideUnitSphere * 0.35f;
                        dir.y += Random.Range(0.4f, 1.2f);
                        dir.Normalize();

                        float force = Random.Range(burstForceMin, burstForceMax);
                        rb.linearVelocity = dir * force + Vector3.up * upwardLift;
                        rb.angularVelocity = Random.insideUnitSphere * torqueStrength;
                    }

                    voxel.BeginDissolve(dissolveDuration);
                }
            }

            // 3. Drive Dissolve progression & Light fade over 2.0s
            float elapsed = 0.0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dissolveDuration);

                // Quadratic curve for dramatic sudden burst and gradual data fade
                float dissolveVal = Mathf.Pow(t, 1.25f);
                Shader.SetGlobalFloat("_VFX_EnemyDeRez_Dissolve", dissolveVal);

                if (burstLight != null)
                {
                    burstLight.intensity = Mathf.Lerp(5.0f, 0.0f, t);
                }

                yield return null;
            }

            if (burstLight != null)
            {
                burstLight.enabled = false;
            }

            // Destroy / return to pool after dissolve completes
            Destroy(gameObject, 0.5f);
        }
    }
}
