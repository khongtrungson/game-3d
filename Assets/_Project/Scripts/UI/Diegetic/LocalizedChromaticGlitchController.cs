using System.Collections;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.UI
{
    /// <summary>
    /// FR-37: Controls localized chromatic glitch effects and emissive wireframe edge feedback on meshes and volumes.
    /// Responds to damage, room resets, and state transitions with micro-glitches.
    /// </summary>
    [DisallowMultipleComponent]
    public class LocalizedChromaticGlitchController : MonoBehaviour
    {
        [Header("Target Renderers")]
        [SerializeField] private Renderer[] _targetRenderers;

        [Header("Glitch Timing & Settings (FR-37)")]
        [SerializeField] private float _defaultGlitchDuration = 0.2f;
        [SerializeField] private float _maxGlitchIntensity = 1.0f;
        [SerializeField] private string _intensityPropName = "_GlitchIntensity";

        [Header("Event Channels")]
        [SerializeField] private PlayerStateEventChannelSO _playerStateEvents;

        private MaterialPropertyBlock _propBlock;
        private int _intensityPropId;
        private Coroutine _glitchRoutine;
        private float _currentIntensity = 0f;

        public float CurrentIntensity => _currentIntensity;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            _intensityPropId = Shader.PropertyToID(_intensityPropName);

            if (_targetRenderers == null || _targetRenderers.Length == 0)
            {
                _targetRenderers = GetComponentsInChildren<Renderer>();
            }
        }

        private void OnEnable()
        {
            if (_playerStateEvents != null)
            {
                _playerStateEvents.OnPlayerDamageTaken += HandleDamageTaken;
                _playerStateEvents.OnPlayerDeath += HandlePlayerDeath;
            }
        }

        private void OnDisable()
        {
            if (_playerStateEvents != null)
            {
                _playerStateEvents.OnPlayerDamageTaken -= HandleDamageTaken;
                _playerStateEvents.OnPlayerDeath -= HandlePlayerDeath;
            }
        }

        private void HandleDamageTaken(int damage, Vector3 hitPoint)
        {
            TriggerGlitch(_defaultGlitchDuration * 0.75f, 0.6f);
        }

        private void HandlePlayerDeath()
        {
            TriggerGlitch(_defaultGlitchDuration, _maxGlitchIntensity);
        }

        public void TriggerGlitch(float duration = 0.2f, float intensity = 1.0f)
        {
            if (_glitchRoutine != null)
            {
                StopCoroutine(_glitchRoutine);
            }
            _glitchRoutine = StartCoroutine(GlitchRoutine(duration, intensity));
        }

        private IEnumerator GlitchRoutine(float duration, float targetIntensity)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Sharp spike then decay
                _currentIntensity = Mathf.Lerp(targetIntensity, 0f, t);
                ApplyIntensity(_currentIntensity);
                yield return null;
            }

            _currentIntensity = 0f;
            ApplyIntensity(0f);
            _glitchRoutine = null;
        }

        public void ApplyIntensity(float intensity)
        {
            _currentIntensity = intensity;
            if (_targetRenderers == null) return;

            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                if (_targetRenderers[i] == null) continue;
                _targetRenderers[i].GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(_intensityPropId, intensity);
                _targetRenderers[i].SetPropertyBlock(_propBlock);
            }
        }
    }
}
