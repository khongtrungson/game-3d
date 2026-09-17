using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NullProtocol.Core;

namespace NullProtocol.World
{
    /// <summary>
    /// Interactive Data Core Terminal required to unlock the subsector extraction gateway (FR-32).
    /// Player interacts by holding or pressing the [F] key within the interaction radius.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class DataCoreTerminal : MonoBehaviour, IResettable
    {
        private static readonly List<DataCoreTerminal> _activeTerminals = new List<DataCoreTerminal>();

        [Header("Terminal Identity")]
        [SerializeField] private string _terminalId = "DataCore_01";
        [SerializeField] private string _displayName = "Data Core Alpha";

        [Header("Interaction Settings (FR-32)")]
        [Tooltip("Maximum distance for player interaction in meters")]
        [SerializeField] private float _interactionRadius = 3.0f;

        [Tooltip("Time in seconds required to hack the terminal (hold F)")]
        [SerializeField] private float _hackDuration = 1.5f;

        [Header("Target Extraction Gateway")]
        [SerializeField] private ExtractionGateway _targetGateway;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer _terminalRenderer;
        [SerializeField] private Color _lockedColor = new Color(1f, 0.3f, 0.1f); // Amber/Red
        [SerializeField] private Color _hackedColor = new Color(0.1f, 1f, 0.8f); // Glowing Cyan

        private bool _isHacked;
        private bool _isInteracting;
        private float _currentHackTimer;
        private MaterialPropertyBlock _propBlock;

        public static IReadOnlyList<DataCoreTerminal> ActiveTerminals => _activeTerminals;

        public string TerminalId => _terminalId;
        public string DisplayName => _displayName;
        public float InteractionRadius => _interactionRadius;
        public float HackDuration => _hackDuration;
        public bool IsHacked => _isHacked;
        public bool IsInteracting => _isInteracting;
        public float HackProgress => (_hackDuration > 0f) ? Mathf.Clamp01(_currentHackTimer / _hackDuration) : (_isHacked ? 1f : 0f);
        public ExtractionGateway TargetGateway { get => _targetGateway; set => _targetGateway = value; }

        public event Action<DataCoreTerminal> OnHackStarted;
        public event Action<DataCoreTerminal, float> OnHackProgressChanged; // terminal, progress (0-1)
        public event Action<DataCoreTerminal> OnHackCompleted;

        private void OnEnable()
        {
            if (!_activeTerminals.Contains(this))
            {
                _activeTerminals.Add(this);
            }
        }

        private void OnDisable()
        {
            _activeTerminals.Remove(this);
        }

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            UpdateVisualFeedback();

            InteractionState.IsNearOrUsingTerminal = () =>
            {
                var cam = Camera.main;
                if (cam != null) return IsAnyTerminalInRange(cam.transform.position);
                var player = GameObject.FindWithTag(Tags.Player);
                return player != null && IsAnyTerminalInRange(player.transform.position);
            };
        }

        private void Start()
        {
            if (_targetGateway != null)
            {
                _targetGateway.RegisterRequiredTerminal(this);
            }
        }

        private void Update()
        {
            if (_isHacked) return;

            CheckInputAndProximity();
        }

        /// <summary>
        /// Checks whether any terminal is currently in interaction range of the given position.
        /// </summary>
        public static bool IsAnyTerminalInRange(Vector3 position, float customRadius = -1f)
        {
            for (int i = 0; i < _activeTerminals.Count; i++)
            {
                var t = _activeTerminals[i];
                if (t != null && !t.IsHacked)
                {
                    float rad = customRadius > 0f ? customRadius : t.InteractionRadius;
                    if (Vector3.Distance(position, t.transform.position) <= rad)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void CheckInputAndProximity()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Check if player camera or body is in range
            Transform playerTransform = GetPlayerTransform();
            if (playerTransform == null) return;

            float dist = Vector3.Distance(playerTransform.position, transform.position);
            bool inRange = dist <= _interactionRadius;

            // FR-32: Interaction via F key
            bool fKeyPressed = keyboard[Key.F].isPressed;

            if (inRange && fKeyPressed)
            {
                if (!_isInteracting)
                {
                    _isInteracting = true;
                    OnHackStarted?.Invoke(this);
                    NullLog.Info("Terminal", $"Hacking started on {_displayName} via [F] key.");
                }

                _currentHackTimer += Time.deltaTime;
                OnHackProgressChanged?.Invoke(this, HackProgress);

                if (_currentHackTimer >= _hackDuration)
                {
                    CompleteHack();
                }
            }
            else
            {
                if (_isInteracting)
                {
                    _isInteracting = false;
                    _currentHackTimer = 0f;
                    OnHackProgressChanged?.Invoke(this, 0f);
                    NullLog.Info("Terminal", $"Hacking interrupted on {_displayName}.");
                }
            }
        }

        /// <summary>
        /// Instantly completes the hack (useful for testing or instant terminals).
        /// </summary>
        public void CompleteHack()
        {
            if (_isHacked) return;

            _isHacked = true;
            _isInteracting = false;
            _currentHackTimer = _hackDuration;

            UpdateVisualFeedback();
            NullLog.Info("Terminal", $"Data Core hacked: {_displayName}. Gateway protocol unlocked!");
            OnHackCompleted?.Invoke(this);

            if (_targetGateway != null)
            {
                _targetGateway.NotifyTerminalSecured(this);
            }
        }

        public void ResetState()
        {
            _isHacked = false;
            _isInteracting = false;
            _currentHackTimer = 0f;
            UpdateVisualFeedback();
            NullLog.Info("Terminal", $"Terminal reset: {_displayName}");
        }

        private void UpdateVisualFeedback()
        {
            if (_terminalRenderer == null) return;

            _terminalRenderer.GetPropertyBlock(_propBlock);
            Color targetColor = _isHacked ? _hackedColor : _lockedColor;
            _propBlock.SetColor("_BaseColor", targetColor);
            _propBlock.SetColor("_EmissionColor", targetColor * 2.0f);
            _terminalRenderer.SetPropertyBlock(_propBlock);
        }

        private Transform GetPlayerTransform()
        {
            var camera = Camera.main;
            if (camera != null) return camera.transform;

            var player = GameObject.FindWithTag(Tags.Player);
            return player != null ? player.transform : null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isHacked ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);
        }
#endif
    }
}
