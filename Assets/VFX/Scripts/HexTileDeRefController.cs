using System.Collections;
using UnityEngine;

namespace NullProtocol.VFX
{
    /// <summary>
    /// Controls the Root Core Floor Tile De-Reference & Void Plunge sequence.
    /// Manages initial glitch shudder, seam burst particle detonation,
    /// dynamic physics drop into the digital void, and deregistration/cleanup.
    /// </summary>
    public class HexTileDeRefController : MonoBehaviour
    {
        [Header("Timing Settings")]
        [Tooltip("Warning shudder / glitch duration before de-referencing release.")]
        [SerializeField] private float warningDuration = 0.6f;

        [Tooltip("Total lifespan before object cleanup in the void.")]
        [SerializeField] private float totalLifespan = 4.0f;

        [Header("Physics Settings")]
        [Tooltip("Downward plunge acceleration impulse applied upon dereference.")]
        [SerializeField] private float plungeImpulse = 4.5f;

        [Tooltip("Random angular torque applied to tilt and destabilize the hex slab.")]
        [SerializeField] private float tumbleTorque = 18.0f;

        [Tooltip("Additional void gravitational pull down (gravity acceleration modifier).")]
        [SerializeField] private float voidGravityMultiplier = 2.2f;

        [Header("Components & References")]
        [Tooltip("The Rigidbody attached to the hex tile slab.")]
        [SerializeField] private Rigidbody tileRigidbody;

        [Tooltip("The MeshRenderer of the hex tile.")]
        [SerializeField] private MeshRenderer tileMeshRenderer;

        [Tooltip("Seam glitch burst particle system.")]
        [SerializeField] private ParticleSystem seamGlitchBurst;

        [Tooltip("Disintegration / void wake particles following the falling tile.")]
        [SerializeField] private ParticleSystem voidWakeParticles;

        [Tooltip("Digital void abyss aura glowing below the tile.")]
        [SerializeField] private ParticleSystem voidAbyssAura;

        [Tooltip("De-referencing glitch flash light.")]
        [SerializeField] private Light glitchLight;

        [Tooltip("Audio source for cyber deref / collapse sound.")]
        [SerializeField] private AudioSource audioSource;

        private MaterialPropertyBlock propBlock;
        private static readonly int DissolveAmountProp = Shader.PropertyToID("_DissolveAmount");
        private static readonly int GlowStrengthProp = Shader.PropertyToID("_GlowStrength");

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool isPlunging = false;

        private void Awake()
        {
            propBlock = new MaterialPropertyBlock();
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            if (tileRigidbody != null)
            {
                tileRigidbody.isKinematic = true;
                tileRigidbody.useGravity = false;
            }
        }

        private void Start()
        {
            TriggerDeReference();
        }

        /// <summary>
        /// Initiates the full de-reference and void plunge sequence.
        /// </summary>
        public void TriggerDeReference()
        {
            StartCoroutine(DeReferenceRoutine());
        }

        private IEnumerator DeReferenceRoutine()
        {
            float elapsed = 0.0f;

            // 1. WARNING / SEAM INSTABILITY PHASE
            // The tile shudders along X/Z plane and seams surge with emissive energy
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }

            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / warningDuration;

                // High-frequency jitter
                float jitterStrength = Mathf.Lerp(0.01f, 0.045f, progress);
                Vector3 jitter = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 45f, 0f) - 0.5f) * 2f * jitterStrength,
                    (Mathf.PerlinNoise(0f, Time.time * 45f) - 0.5f) * 0.5f * jitterStrength,
                    (Mathf.PerlinNoise(Time.time * 45f, 10f) - 0.5f) * 2f * jitterStrength
                );
                transform.position = initialPosition + jitter;

                // Emissive glitch pulse
                if (tileMeshRenderer != null)
                {
                    tileMeshRenderer.GetPropertyBlock(propBlock);
                    float glow = Mathf.Lerp(3.0f, 15.0f, Mathf.PingPong(progress * 8.0f, 1.0f));
                    propBlock.SetFloat(GlowStrengthProp, glow);
                    tileMeshRenderer.SetPropertyBlock(propBlock);
                }

                if (glitchLight != null)
                {
                    glitchLight.intensity = Mathf.Lerp(1.0f, 6.0f, progress) * Random.Range(0.8f, 1.2f);
                }

                yield return null;
            }

            // 2. DETONATION & SEAM BURST MOMENT
            if (seamGlitchBurst != null)
            {
                seamGlitchBurst.Play();
            }

            if (voidWakeParticles != null)
            {
                voidWakeParticles.Play();
            }

            if (voidAbyssAura != null)
            {
                voidAbyssAura.Play();
            }

            // Release physics constraints
            isPlunging = true;
            if (tileRigidbody != null)
            {
                tileRigidbody.isKinematic = false;
                tileRigidbody.useGravity = true;

                // Apply initial downward plunge impulse & destabilizing torque
                Vector3 impulse = Vector3.down * plungeImpulse;
                // Add slight asymmetric kick
                impulse += new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f));
                tileRigidbody.AddForce(impulse, ForceMode.Impulse);

                Vector3 torque = new Vector3(
                    Random.Range(-tumbleTorque, tumbleTorque),
                    Random.Range(-tumbleTorque * 0.5f, tumbleTorque * 0.5f),
                    Random.Range(-tumbleTorque, tumbleTorque)
                );
                tileRigidbody.AddTorque(torque, ForceMode.Impulse);
            }

            // 3. DISSOLVE INTO THE DIGITAL VOID
            float dissolveTime = 0.0f;
            float plungeDuration = totalLifespan - warningDuration;

            while (dissolveTime < plungeDuration)
            {
                dissolveTime += Time.deltaTime;
                float t = Mathf.Clamp01(dissolveTime / plungeDuration);

                // Progressive deref dissolve
                if (tileMeshRenderer != null)
                {
                    tileMeshRenderer.GetPropertyBlock(propBlock);
                    // Dissolve starts kicking in after initial fall
                    float dissolveVal = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01((t - 0.2f) / 0.8f));
                    propBlock.SetFloat(DissolveAmountProp, dissolveVal);
                    tileMeshRenderer.SetPropertyBlock(propBlock);
                }

                // Fade out glitch light
                if (glitchLight != null)
                {
                    glitchLight.intensity = Mathf.Lerp(6.0f, 0.0f, t * 2.5f);
                }

                yield return null;
            }

            // 4. CLEANUP
            Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (isPlunging && tileRigidbody != null && !tileRigidbody.isKinematic)
            {
                // Extra void gravitational acceleration downwards
                tileRigidbody.AddForce(Physics.gravity * (voidGravityMultiplier - 1.0f), ForceMode.Acceleration);
            }
        }
    }
}
