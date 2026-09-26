using System;
using System.Collections;
using UnityEngine;

namespace NullProtocol.VFX
{
    /// <summary>
    /// Controls the Logic-Trip Stasis Lockdown Freeze Effect.
    /// Specifications:
    /// - Encasing enemy in rigid cyan wireframe geometric lattice for exactly 5.0s.
    /// - Supports both:
    ///   1. Standalone / Mesh Overlay mode (instantiated at target position encasing hostile).
    ///   2. Material Swap mode (dynamically swap enemy renderers' materials to stasis wireframe for 5.0s).
    /// - 0.0s: Immediate high-intensity cyan stasis snap flash + point light pulse.
    /// - 0.0s - 5.0s: Rigid freeze constraint; pulsing geometric scanlines & orbiting constraint data glyphs.
    /// - 5.0s: Violent lattice shatter / dissolve burst returning target or de-rezzing upon release.
    /// </summary>
    [DisallowMultipleComponent]
    public class StasisLockdownController : MonoBehaviour
    {
        [Header("Stasis Duration Specs")]
        [Tooltip("Exact lockdown duration specified by GDD §8 / PRD §4.3 (FR-21).")]
        [SerializeField] private float stasisDuration = 5.0f;

        [Header("Visual Components")]
        [SerializeField] private MeshRenderer humanoidOverlayRenderer;
        [SerializeField] private MeshRenderer hexCageRenderer;
        [SerializeField] private ParticleSystem initiationFlashParticles;
        [SerializeField] private ParticleSystem constraintGlyphParticles;
        [SerializeField] private ParticleSystem scanlineStaticParticles;
        [SerializeField] private ParticleSystem shatterBurstParticles;
        [SerializeField] private Light stasisLight;
        [SerializeField] private AudioSource sfxSource;

        [Header("Material Swap References (Optional)")]
        [SerializeField] private Material stasisWireframeMaterial;

        // Internal tracking
        private Renderer[] targetOriginalRenderers;
        private Material[][] cachedOriginalMaterials;
        private Coroutine stasisRoutine;

        public event Action OnStasisInitiated;
        public event Action OnStasisExpired;

        private void Start()
        {
            // Auto-run stasis timer upon instantiation
            stasisRoutine = StartCoroutine(RoutineStasisLifecycle());
        }

        /// <summary>
        /// Applies stasis lockdown to a target GameObject, performing material swap and locking animator/physics.
        /// </summary>
        public void ApplyStasisToTarget(GameObject target)
        {
            if (target == null) return;

            // 1. Freeze Rigidbody velocity if present
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // 2. Freeze Animator if present
            Animator anim = target.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.speed = 0.0f;
            }

            // 3. Material swap to stasis wireframe
            if (stasisWireframeMaterial != null)
            {
                targetOriginalRenderers = target.GetComponentsInChildren<Renderer>();
                cachedOriginalMaterials = new Material[targetOriginalRenderers.Length][];

                for (int i = 0; i < targetOriginalRenderers.Length; i++)
                {
                    Renderer rend = targetOriginalRenderers[i];
                    cachedOriginalMaterials[i] = rend.sharedMaterials;

                    Material[] wireMats = new Material[rend.sharedMaterials.Length];
                    for (int m = 0; m < wireMats.Length; m++)
                    {
                        wireMats[m] = stasisWireframeMaterial;
                    }
                    rend.materials = wireMats;
                }
            }
        }

        private IEnumerator RoutineStasisLifecycle()
        {
            OnStasisInitiated?.Invoke();

            // 1. Initial 0.0s Immediate Snap Flash
            if (initiationFlashParticles != null) initiationFlashParticles.Play();
            if (constraintGlyphParticles != null) constraintGlyphParticles.Play();
            if (scanlineStaticParticles != null) scanlineStaticParticles.Play();
            if (sfxSource != null) sfxSource.Play();

            if (stasisLight != null)
            {
                stasisLight.enabled = true;
                stasisLight.intensity = 5.0f;
            }

            // 2. Hold rigid stasis for exactly 5.0 seconds
            float elapsed = 0.0f;
            while (elapsed < stasisDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / stasisDuration;

                // Subtle high-frequency digital lattice hum pulse
                if (stasisLight != null)
                {
                    float pulse = 2.5f + 0.8f * Mathf.Sin(elapsed * 12.0f);
                    stasisLight.intensity = pulse;
                }

                yield return null;
            }

            // 3. Exactly 5.0s Completion: Shatter Burst Release
            if (shatterBurstParticles != null)
            {
                shatterBurstParticles.Play();
            }

            if (humanoidOverlayRenderer != null) humanoidOverlayRenderer.enabled = false;
            if (hexCageRenderer != null) hexCageRenderer.enabled = false;
            if (constraintGlyphParticles != null) constraintGlyphParticles.Stop();
            if (scanlineStaticParticles != null) scanlineStaticParticles.Stop();

            if (stasisLight != null)
            {
                stasisLight.intensity = 6.0f; // Terminal flash
                yield return new WaitForSeconds(0.12f);
                stasisLight.enabled = false;
            }

            // Revert original materials if swap mode was applied
            RevertTargetMaterials();

            OnStasisExpired?.Invoke();

            // Destroy effect instance after shatter particles clear
            Destroy(gameObject, 1.2f);
        }

        private void RevertTargetMaterials()
        {
            if (targetOriginalRenderers == null || cachedOriginalMaterials == null) return;

            for (int i = 0; i < targetOriginalRenderers.Length; i++)
            {
                Renderer rend = targetOriginalRenderers[i];
                if (rend != null && cachedOriginalMaterials[i] != null)
                {
                    rend.materials = cachedOriginalMaterials[i];
                }
            }
        }
    }
}
