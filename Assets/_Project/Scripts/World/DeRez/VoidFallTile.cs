using System;
using System.Collections;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.World
{
    public enum TileDeRezState
    {
        Solid,
        WarningGlitch,
        Falling,
        DeReferenced
    }

    /// <summary>
    /// Individual floor tile in the Root Core arena that can de-reference and collapse into the void (FR-31).
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class VoidFallTile : MonoBehaviour, IResettable
    {
        [Header("Tile State (FR-31)")]
        [SerializeField] private TileDeRezState _state = TileDeRezState.Solid;
        [SerializeField] private int _waveIndex = 1; // 1, 2, or 3

        [Header("Physics & Motion")]
        [SerializeField] private float _fallSpeed = 12.0f;
        [SerializeField] private float _fallDuration = 2.5f;

        [Header("Components")]
        [SerializeField] private Collider _collider;
        [SerializeField] private Renderer _renderer;

        [Header("Colors & Visuals")]
        [SerializeField] private Color _solidColor = new Color(0.2f, 0.25f, 0.3f);
        [SerializeField] private Color _warningColor = new Color(1f, 0.15f, 0.05f); // Neon Amber/Red

        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Coroutine _actionRoutine;
        private MaterialPropertyBlock _propBlock;

        public TileDeRezState State => _state;
        public int WaveIndex { get => _waveIndex; set => _waveIndex = value; }
        public bool IsActiveInArena => _state == TileDeRezState.Solid || _state == TileDeRezState.WarningGlitch;

        public event Action<VoidFallTile> OnWarningStarted;
        public event Action<VoidFallTile> OnFallen;

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _propBlock = new MaterialPropertyBlock();

            if (_collider == null) _collider = GetComponent<Collider>();
            if (_renderer == null) _renderer = GetComponent<Renderer>();

            SetVisualState(_solidColor);
        }

        /// <summary>
        /// Starts the warning glitch phase before dropping into the void.
        /// </summary>
        public void StartWarningPhase(float warningDuration)
        {
            if (_state != TileDeRezState.Solid) return;

            if (_actionRoutine != null) StopCoroutine(_actionRoutine);
            _actionRoutine = StartCoroutine(WarningAndDropRoutine(warningDuration));
        }

        private IEnumerator WarningAndDropRoutine(float warningDuration)
        {
            _state = TileDeRezState.WarningGlitch;
            OnWarningStarted?.Invoke(this);

            float elapsed = 0f;
            Vector3 basePos = _initialLocalPosition;

            // Warning flicker & vibration
            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / warningDuration;

                // Emissive flicker frequency increases over time
                float flicker = Mathf.Sin(elapsed * (10f + progress * 20f));
                Color color = Color.Lerp(_solidColor, _warningColor, (flicker > 0f) ? 0.9f : 0.2f);
                SetVisualState(color);

                // Slight micro-jitter
                Vector3 jitter = UnityEngine.Random.insideUnitSphere * (progress * 0.04f);
                jitter.y = 0f;
                transform.localPosition = basePos + jitter;

                yield return null;
            }

            // Begin falling into the void
            yield return StartCoroutine(DropIntoVoidRoutine());
        }

        /// <summary>
        /// Immediately drops the tile into the void without warning (useful for testing or instant collapse).
        /// </summary>
        public void DropImmediately()
        {
            if (_actionRoutine != null) StopCoroutine(_actionRoutine);
            _actionRoutine = StartCoroutine(DropIntoVoidRoutine());
        }

        private IEnumerator DropIntoVoidRoutine()
        {
            _state = TileDeRezState.Falling;

            // Disable collision so player or actors fall through
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            SetVisualState(_warningColor * 1.5f);

            float fallElapsed = 0f;
            float currentVelocity = 0f;

            while (fallElapsed < _fallDuration)
            {
                fallElapsed += Time.deltaTime;
                currentVelocity += _fallSpeed * Time.deltaTime;
                transform.position += Vector3.down * (currentVelocity * Time.deltaTime);

                yield return null;
            }

            _state = TileDeRezState.DeReferenced;
            gameObject.SetActive(false);
            OnFallen?.Invoke(this);
        }

        public void ResetState()
        {
            if (_actionRoutine != null)
            {
                StopCoroutine(_actionRoutine);
                _actionRoutine = null;
            }

            gameObject.SetActive(true);
            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            _state = TileDeRezState.Solid;
            SetVisualState(_solidColor);
        }

        private void SetVisualState(Color color)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", color);
            _propBlock.SetColor("_EmissionColor", color);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
