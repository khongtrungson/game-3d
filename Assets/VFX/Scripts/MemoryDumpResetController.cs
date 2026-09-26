using System;
using System.Collections;
using UnityEngine;

namespace NullProtocol.VFX
{
    /// <summary>
    /// Controls the Memory-Dump Fast Reset Transition Glitch (VFX-MEMORYDUMP-RESET).
    /// Specifications:
    /// - Triggered when player reaches 0 HP.
    /// - Post-process digital glitch collapse returning camera to room threshold within 2.0s.
    /// - Strict 2.0s timing budget with zero scene unload / hitching:
    ///   * Phase 1 (0.00s - 0.45s): Collapse & Glitch Peak (RGB split, horizontal tearing, terminal readout, audio stinger).
    ///   * Phase 2 (0.45s - 1.25s): De-referenced Blackout & Seamless Camera Warp to room threshold marker.
    ///   * Phase 3 (1.25s - 2.00s): Restoration & Glitch Clear (HUD re-initializes, controls restored at exactly t=2.00s).
    /// </summary>
    [DisallowMultipleComponent]
    public class MemoryDumpResetController : MonoBehaviour
    {
        [Header("Transition Budget Timing (Strict 2.0s)")]
        [Tooltip("Total duration of the entire reset sequence in seconds.")]
        [SerializeField] private float totalDuration = 2.0f;

        [Tooltip("Time point where blackout is reached and camera warp occurs (Phase 1 -> 2).")]
        [SerializeField] private float warpTime = 0.80f;

        [Tooltip("Time point where screen begins de-glitching and restoring (Phase 2 -> 3).")]
        [SerializeField] private float recoverTime = 1.25f;

        [Header("Camera & Threshold Anchors")]
        [Tooltip("Target player camera to relocate and shake.")]
        [SerializeField] private Transform playerCamera;

        [Tooltip("Transform of the Room Threshold Marker (e.g. ENV_MOD_15_ThresholdMarker) where player respawns.")]
        [SerializeField] private Transform roomThresholdMarker;

        [Tooltip("Fallback reset position if no room threshold marker is assigned.")]
        [SerializeField] private Vector3 fallbackThresholdPosition = Vector3.zero;

        [Tooltip("Fallback reset rotation if no room threshold marker is assigned.")]
        [SerializeField] private Vector3 fallbackThresholdEuler = Vector3.zero;

        [Header("Post-Processing & Overlay Renderers")]
        [Tooltip("URP Post-Processing Glitch Material (MAT_Glitch_Post).")]
        [SerializeField] private Material glitchPostMaterial;

        [Tooltip("Screen-space billboard quad mesh for direct viewport fallback.")]
        [SerializeField] private MeshRenderer screenOverlayRenderer;

        [Tooltip("Diegetic monospace terminal text renderer.")]
        [SerializeField] private MeshRenderer terminalReadoutRenderer;

        [Tooltip("Corner glitch vignette renderer.")]
        [SerializeField] private MeshRenderer vignetteRenderer;

        [Header("Particle Emitters")]
        [SerializeField] private ParticleSystem bitstreamDebrisParticles;
        [SerializeField] private ParticleSystem radialCollapseShockwave;

        [Header("Lighting & Audio")]
        [SerializeField] private Light emergencyGlitchLight;
        [SerializeField] private AudioSource deathStingerSource;
        [SerializeField] private AudioClip deathStingerClip;

        [Header("Debug")]
        [SerializeField] private bool triggerOnStart = false;
        [SerializeField] private KeyCode testTriggerKey = KeyCode.K;

        // Shader Property IDs
        private static readonly int PropGlitchIntensity = Shader.PropertyToID("_GlitchIntensity");
        private static readonly int PropRGBSplit = Shader.PropertyToID("_RGBSplit");
        private static readonly int PropHorizontalDisplacement = Shader.PropertyToID("_HorizontalDisplacement");
        private static readonly int PropDeRefCollapse = Shader.PropertyToID("_DeRefCollapse");
        private static readonly int PropFlashIntensity = Shader.PropertyToID("_FlashIntensity");

        // Events
        public event Action OnResetInitiated;
        public event Action OnCameraThresholdWarp;
        public event Action OnResetCompleted;

        private Coroutine resetRoutine;
        private bool isResetting = false;

        public bool IsResetting => isResetting;

        private void Awake()
        {
            // Auto-locate camera if not assigned
            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }

            // Ensure visual components start disabled
            SetGlitchVisualsActive(false);
            UpdateShaderParameters(0.0f, 0.0f, 0.0f, 0.0f);
        }

        private void Start()
        {
            if (triggerOnStart)
            {
                TriggerReset();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(testTriggerKey) && !isResetting)
            {
                TriggerReset();
            }
        }

        /// <summary>
        /// Initiates the full 2.0s fast memory dump reset transition loop.
        /// </summary>
        public void TriggerReset(Transform targetCamera = null, Vector3? thresholdPos = null, Quaternion? thresholdRot = null, Action onComplete = null)
        {
            if (isResetting) return;

            if (targetCamera != null) playerCamera = targetCamera;
            if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

            Vector3 targetPos = thresholdPos ?? (roomThresholdMarker != null ? roomThresholdMarker.position : fallbackThresholdPosition);
            Quaternion targetRot = thresholdRot ?? (roomThresholdMarker != null ? roomThresholdMarker.rotation : Quaternion.Euler(fallbackThresholdEuler));

            resetRoutine = StartCoroutine(RoutineExecuteReset(targetPos, targetRot, onComplete));
        }

        /// <summary>
        /// Binds the controller directly to a player health component.
        /// When HP <= 0, triggers reset automatically.
        /// </summary>
        public void OnPlayerDeathTrigger()
        {
            if (!isResetting)
            {
                TriggerReset();
            }
        }

        /// <summary>
        /// Registers a dynamic room threshold marker (e.g. upon entering a new combat room).
        /// </summary>
        public void SetRoomThreshold(Transform marker)
        {
            roomThresholdMarker = marker;
        }

        private IEnumerator RoutineExecuteReset(Vector3 targetPos, Quaternion targetRot, Action onComplete)
        {
            isResetting = true;
            OnResetInitiated?.Invoke();

            SetGlitchVisualsActive(true);

            // Play 1.8s audio stinger
            if (deathStingerSource != null)
            {
                if (deathStingerClip != null && deathStingerSource.clip == null)
                {
                    deathStingerSource.clip = deathStingerClip;
                }
                deathStingerSource.Play();
            }

            // Play particles
            if (radialCollapseShockwave != null) radialCollapseShockwave.Play();
            if (bitstreamDebrisParticles != null) bitstreamDebrisParticles.Play();

            // Cache camera start transform
            Vector3 camStartPos = playerCamera != null ? playerCamera.position : Vector3.zero;
            Quaternion camStartRot = playerCamera != null ? playerCamera.rotation : Quaternion.identity;

            float elapsed = 0.0f;

            // -------------------------------------------------------------
            // Phase 1 (0.00s - 0.45s): Collapse & Glitch Peak
            // -------------------------------------------------------------
            while (elapsed < 0.45f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.45f);

                float intensity = Mathf.SmoothStep(0.0f, 1.0f, t);
                float flash = Mathf.Max(0.0f, (1.0f - t * 2.0f)) * 3.5f;
                float deRef = Mathf.Clamp01((elapsed - 0.25f) / 0.20f);

                UpdateShaderParameters(intensity, flash, deRef, t);

                if (emergencyGlitchLight != null)
                {
                    emergencyGlitchLight.intensity = (flash + UnityEngine.Random.Range(0.0f, 2.0f)) * intensity;
                }

                // Minor camera glitch jitter
                if (playerCamera != null)
                {
                    Vector3 jitter = UnityEngine.Random.insideUnitSphere * (0.08f * intensity);
                    playerCamera.position = camStartPos + jitter;
                }

                yield return null;
            }

            // -------------------------------------------------------------
            // Phase 2 (0.45s - 1.25s): De-Referenced Blackout & Seamless Warp
            // -------------------------------------------------------------
            bool warped = false;

            while (elapsed < recoverTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float tPhase2 = Mathf.Clamp01((elapsed - 0.45f) / (recoverTime - 0.45f));

                // Maintain full blackout / heavy glitch overlay to conceal warp
                UpdateShaderParameters(1.0f, 0.0f, 1.0f, 1.0f);

                // Perform camera & player warp at warpTime (0.80s)
                if (!warped && elapsed >= warpTime)
                {
                    if (playerCamera != null)
                    {
                        // Warp player root if camera is child of player
                        Transform playerRoot = playerCamera.root != playerCamera ? playerCamera.root : playerCamera;
                        playerRoot.position = targetPos;
                        playerRoot.rotation = targetRot;

                        // Zero out velocities if Rigidbody is attached
                        Rigidbody rb = playerRoot.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }

                        // Reset CharacterController if present
                        CharacterController cc = playerRoot.GetComponent<CharacterController>();
                        if (cc != null)
                        {
                            cc.enabled = false;
                            playerRoot.position = targetPos;
                            cc.enabled = true;
                        }
                    }

                    warped = true;
                    OnCameraThresholdWarp?.Invoke();
                }

                yield return null;
            }

            // -------------------------------------------------------------
            // Phase 3 (1.25s - 2.00s): Restoration & Glitch Clear
            // -------------------------------------------------------------
            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float tRecover = Mathf.Clamp01((elapsed - recoverTime) / (totalDuration - recoverTime));

                // Smoothly fade down glitch intensity and blackout to 0
                float intensity = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, tRecover);
                float deRef = 1.0f - Mathf.Clamp01(tRecover * 1.6f);

                UpdateShaderParameters(intensity, 0.0f, deRef, 1.0f - tRecover);

                if (emergencyGlitchLight != null)
                {
                    emergencyGlitchLight.intensity = intensity * 0.5f;
                }

                yield return null;
            }

            // -------------------------------------------------------------
            // Sequence Complete (t = 2.00s strict)
            // -------------------------------------------------------------
            UpdateShaderParameters(0.0f, 0.0f, 0.0f, 0.0f);
            SetGlitchVisualsActive(false);

            if (bitstreamDebrisParticles != null) bitstreamDebrisParticles.Stop();
            if (radialCollapseShockwave != null) radialCollapseShockwave.Stop();

            isResetting = false;
            resetRoutine = null;

            onComplete?.Invoke();
            OnResetCompleted?.Invoke();
        }

        private void UpdateShaderParameters(float intensity, float flash, float deRef, float textAlpha)
        {
            if (glitchPostMaterial != null)
            {
                glitchPostMaterial.SetFloat(PropGlitchIntensity, intensity);
                glitchPostMaterial.SetFloat(PropFlashIntensity, flash);
                glitchPostMaterial.SetFloat(PropDeRefCollapse, deRef);
                glitchPostMaterial.SetFloat(PropRGBSplit, 0.035f * intensity);
                glitchPostMaterial.SetFloat(PropHorizontalDisplacement, 0.045f * intensity);
            }

            // Sync screen quad overlays for direct camera rendering
            if (screenOverlayRenderer != null && screenOverlayRenderer.material != null)
            {
                Color c = screenOverlayRenderer.material.color;
                c.a = intensity * 0.85f;
                screenOverlayRenderer.material.color = c;
            }

            if (terminalReadoutRenderer != null && terminalReadoutRenderer.material != null)
            {
                Color c = terminalReadoutRenderer.material.color;
                c.a = textAlpha;
                terminalReadoutRenderer.material.color = c;
            }

            if (vignetteRenderer != null && vignetteRenderer.material != null)
            {
                Color c = vignetteRenderer.material.color;
                c.a = intensity;
                vignetteRenderer.material.color = c;
            }
        }

        private void SetGlitchVisualsActive(bool active)
        {
            if (screenOverlayRenderer != null) screenOverlayRenderer.enabled = active;
            if (terminalReadoutRenderer != null) terminalReadoutRenderer.enabled = active;
            if (vignetteRenderer != null) vignetteRenderer.enabled = active;
            if (emergencyGlitchLight != null) emergencyGlitchLight.enabled = active;
        }

        public void AbortReset()
        {
            if (resetRoutine != null)
            {
                StopCoroutine(resetRoutine);
                resetRoutine = null;
            }

            UpdateShaderParameters(0.0f, 0.0f, 0.0f, 0.0f);
            SetGlitchVisualsActive(false);
            isResetting = false;
        }
    }
}
