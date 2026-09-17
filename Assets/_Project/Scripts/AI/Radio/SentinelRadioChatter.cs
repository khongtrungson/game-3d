using System;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.AI
{
    public enum RadioChatterType
    {
        AlertStatusChanged,   // Sighted target / status escalation
        SuppressionCallout,   // Pinned down by fire / blinded by smoke
        FlankingCallout,      // Initiating flank maneuver
        DoorStackCallout,     // Ready at breach stack
        BreachingCallout,     // Breaching door now
        CoverRelocatingCallout, // Moving between cover nodes
        ShieldAdvancingCallout  // Shield Breacher leading push
    }

    /// <summary>
    /// Event channel for 3D spatialized AI radio chatter (FR-27).
    /// Audio systems or test listeners can subscribe to play HRTF spatial cues.
    /// </summary>
    [CreateAssetMenu(fileName = "AIRadioChatterEventChannel", menuName = "NullProtocol/Events/AI Radio Chatter Event Channel")]
    public class AIRadioChatterEventChannelSO : ScriptableObject
    {
        public event Action<Vector3, RadioChatterType, string> OnChatterEmitted;

        public void RaiseChatter(Vector3 position, RadioChatterType chatterType, string message)
        {
            OnChatterEmitted?.Invoke(position, chatterType, message);
        }
    }

    /// <summary>
    /// 3D Spatialized Synthetic Radio Chatter Emitter (FR-27).
    /// Emits tactical audio callouts with pitch-modulated synthetic voice lines.
    /// Integrated with Unity AudioSource configured for 3D spatial spread.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class SentinelRadioChatter : MonoBehaviour
    {
        [Header("Event Channel")]
        [SerializeField] private AIRadioChatterEventChannelSO _radioChannel;

        [Header("Audio Settings (FR-27, FR-39)")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("Minimum time between chatter callouts to prevent audio spam (seconds)")]
        [SerializeField] private float _chatterCooldown = 2.5f;

        [Tooltip("Audio clips for chatter categories")]
        [SerializeField] private AudioClip _alertClip;
        [SerializeField] private AudioClip _suppressionClip;
        [SerializeField] private AudioClip _flankingClip;
        [SerializeField] private AudioClip _doorStackClip;
        [SerializeField] private AudioClip _breachClip;

        private float _lastChatterTime = -99f;
        private string _lastChatterMessage = string.Empty;
        private RadioChatterType _lastChatterType;

        public string LastChatterMessage => _lastChatterMessage;
        public RadioChatterType LastChatterType => _lastChatterType;
        public float LastChatterTime => _lastChatterTime;

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_audioSource != null)
            {
                _audioSource.spatialBlend = 1.0f; // 100% 3D spatialized (HRTF binaural)
                _audioSource.rolloffMode = AudioRolloffMode.Linear;
                _audioSource.minDistance = 2.0f;
                _audioSource.maxDistance = 25.0f;
                _audioSource.playOnAwake = false;
            }
        }

        public void SetRadioChannel(AIRadioChatterEventChannelSO channel)
        {
            _radioChannel = channel;
        }

        public bool EmitChatter(RadioChatterType chatterType, string customMessage = null, bool force = false)
        {
            if (!force && Time.time - _lastChatterTime < _chatterCooldown)
            {
                return false;
            }

            _lastChatterTime = Time.time;
            _lastChatterType = chatterType;

            string msg = customMessage ?? GetDefaultMessage(chatterType);
            _lastChatterMessage = msg;

            NullLog.Info("RadioChatter", $"[{gameObject.name}] {chatterType}: \"{msg}\"");

            // Play spatialized clip if available
            AudioClip clip = GetClipForType(chatterType);
            if (clip != null && _audioSource != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f); // synthetic modulator
                _audioSource.PlayOneShot(clip);
            }

            _radioChannel?.RaiseChatter(transform.position, chatterType, msg);
            return true;
        }

        private AudioClip GetClipForType(RadioChatterType type)
        {
            switch (type)
            {
                case RadioChatterType.AlertStatusChanged:
                    return _alertClip;
                case RadioChatterType.SuppressionCallout:
                    return _suppressionClip;
                case RadioChatterType.FlankingCallout:
                    return _flankingClip;
                case RadioChatterType.DoorStackCallout:
                    return _doorStackClip;
                case RadioChatterType.BreachingCallout:
                    return _breachClip;
                default:
                    return null;
            }
        }

        private string GetDefaultMessage(RadioChatterType type)
        {
            switch (type)
            {
                case RadioChatterType.AlertStatusChanged:
                    return "Hostile contact confirmed. Engaging.";
                case RadioChatterType.SuppressionCallout:
                    return "Heavy suppression! Visual compromised!";
                case RadioChatterType.FlankingCallout:
                    return "Executing dynamic flanking route.";
                case RadioChatterType.DoorStackCallout:
                    return "Stacked at threshold. Awaiting breach cue.";
                case RadioChatterType.BreachingCallout:
                    return "Breaching room now! Clear angles!";
                case RadioChatterType.CoverRelocatingCallout:
                    return "Relocating to defensive cover node.";
                case RadioChatterType.ShieldAdvancingCallout:
                    return "Shield leading wedge. Advance.";
                default:
                    return "Radio check.";
            }
        }
    }
}
