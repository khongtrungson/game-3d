using System;
using System.Collections.Generic;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.World
{
    /// <summary>
    /// Security gateway that remains locked until required Data Core terminals are hacked (FR-32).
    /// Stepping into the unlocked gateway triggers level extraction and tactical debrief (FR-33).
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class ExtractionGateway : MonoBehaviour, IResettable
    {
        [Header("Gateway Identity")]
        [SerializeField] private string _gatewayId = "Gateway_Alpha";

        [Header("Requirements (FR-32)")]
        [SerializeField] private List<DataCoreTerminal> _requiredTerminals = new List<DataCoreTerminal>();
        [SerializeField] private int _minimumTerminalsRequired = 1;

        [Header("Extraction Zone Settings")]
        [SerializeField] private float _extractionRadius = 2.5f;

        [Header("Physical & Visual Barriers")]
        [SerializeField] private Collider _physicalBarrier;
        [SerializeField] private Renderer _gatewayRenderer;
        [SerializeField] private Color _lockedColor = new Color(0.8f, 0.1f, 0.1f); // Red
        [SerializeField] private Color _unlockedColor = new Color(0.1f, 1f, 0.8f); // Emissive Cyan

        private bool _isUnlocked;
        private bool _hasExtracted;
        private MaterialPropertyBlock _propBlock;

        public string GatewayId => _gatewayId;
        public bool IsUnlocked => _isUnlocked;
        public bool HasExtracted => _hasExtracted;
        public float ExtractionRadius => _extractionRadius;
        public IReadOnlyList<DataCoreTerminal> RequiredTerminals => _requiredTerminals;

        public event Action<ExtractionGateway> OnGatewayUnlocked;
        public event Action<ExtractionGateway> OnExtractionCompleted;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            UpdateVisualFeedback();
        }

        private void Update()
        {
            if (!_isUnlocked)
            {
                CheckTerminalsUnlocked();
            }
            else if (!_hasExtracted)
            {
                CheckPlayerProximity();
            }
        }

        public void RegisterRequiredTerminal(DataCoreTerminal terminal)
        {
            if (terminal != null && !_requiredTerminals.Contains(terminal))
            {
                _requiredTerminals.Add(terminal);
            }
        }

        public void NotifyTerminalSecured(DataCoreTerminal terminal)
        {
            CheckTerminalsUnlocked();
        }

        private void CheckTerminalsUnlocked()
        {
            if (_isUnlocked) return;

            int securedCount = 0;
            for (int i = 0; i < _requiredTerminals.Count; i++)
            {
                if (_requiredTerminals[i] != null && _requiredTerminals[i].IsHacked)
                {
                    securedCount++;
                }
            }

            int needed = Mathf.Max(_minimumTerminalsRequired, _requiredTerminals.Count);
            if (securedCount >= needed)
            {
                Unlock();
            }
        }

        /// <summary>
        /// Unlocks the extraction gateway, opening the pathway and enabling extraction (FR-32).
        /// </summary>
        public void Unlock()
        {
            if (_isUnlocked) return;

            _isUnlocked = true;
            if (_physicalBarrier != null)
            {
                _physicalBarrier.enabled = false;
            }

            UpdateVisualFeedback();
            NullLog.Info("Extraction", $"Gateway unlocked: {_gatewayId}. Extraction portal is active.");
            OnGatewayUnlocked?.Invoke(this);
        }

        private void CheckPlayerProximity()
        {
            var camera = Camera.main;
            Vector3 playerPos = camera != null ? camera.transform.position : Vector3.zero;

            if (camera == null)
            {
                var player = GameObject.FindWithTag(Tags.Player);
                if (player != null) playerPos = player.transform.position;
            }

            if (Vector3.Distance(playerPos, transform.position) <= _extractionRadius)
            {
                TriggerExtraction();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isUnlocked || _hasExtracted) return;

            if (other.CompareTag(Tags.Player) || other.GetComponent<Controller.TacticalLocomotionController>() != null)
            {
                TriggerExtraction();
            }
        }

        /// <summary>
        /// Executes level extraction protocol (FR-32, FR-33).
        /// </summary>
        public void TriggerExtraction()
        {
            if (!_isUnlocked || _hasExtracted) return;

            _hasExtracted = true;
            NullLog.Info("Extraction", $"Extraction successful through {_gatewayId}! Transitioning to Tactical Debrief.");
            OnExtractionCompleted?.Invoke(this);
        }

        public void ResetState()
        {
            _isUnlocked = false;
            _hasExtracted = false;

            if (_physicalBarrier != null)
            {
                _physicalBarrier.enabled = true;
            }

            UpdateVisualFeedback();
            NullLog.Info("Extraction", $"Gateway reset: {_gatewayId}");
        }

        private void UpdateVisualFeedback()
        {
            if (_gatewayRenderer == null) return;

            _gatewayRenderer.GetPropertyBlock(_propBlock);
            Color color = _isUnlocked ? _unlockedColor : _lockedColor;
            _propBlock.SetColor("_BaseColor", color);
            _propBlock.SetColor("_EmissionColor", color * 2.0f);
            _gatewayRenderer.SetPropertyBlock(_propBlock);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isUnlocked ? Color.cyan : Color.red;
            Gizmos.DrawWireSphere(transform.position, _extractionRadius);
        }
#endif
    }
}
