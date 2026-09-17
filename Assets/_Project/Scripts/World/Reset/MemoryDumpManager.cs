using System;
using System.Collections;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.Controller;
using NullProtocol.Combat;
using NullProtocol.Gadgets;

namespace NullProtocol.World
{
    /// <summary>
    /// Coordinates the instantaneous Memory-Dump Loop (FR-30, UJ-5).
    /// Resets the current room within <= 2.0 seconds upon player death without a full scene reload.
    /// </summary>
    [DisallowMultipleComponent]
    public class MemoryDumpManager : MonoBehaviour
    {
        public static MemoryDumpManager Instance { get; private set; }

        [Header("Timing Settings (FR-30: <= 2.0s)")]
        [Tooltip("Total duration of the memory-dump recovery loop in seconds")]
        [SerializeField] private float _resetDuration = 1.5f;

        [Tooltip("Duration of the glitch dissolve fade-in")]
        [SerializeField] private float _glitchFadeDuration = 0.2f;

        [Header("Event Channels")]
        [SerializeField] private PlayerStateEventChannelSO _playerStateEvents;
        [SerializeField] private VoidEventChannelSO _memoryDumpResetEvents;

        [Header("Player References (Optional Auto-Find)")]
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private TacticalLocomotionController _playerLocomotion;
        [SerializeField] private PlayerCombatController _playerCombat;
        [SerializeField] private PlayerGadgetController _playerGadgets;

        [Header("Active Room")]
        [SerializeField] private RoomController _activeRoom;

        private bool _isResetting;
        private Coroutine _resetRoutine;

        public float ResetDuration => _resetDuration;
        public bool IsResetting => _isResetting;
        public RoomController ActiveRoom => _activeRoom;

        public event Action OnMemoryDumpStarted;
        public event Action OnMemoryDumpCompleted;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Strictly clamp reset duration to FR-30 requirement (<= 2.0 seconds)
            _resetDuration = Mathf.Clamp(_resetDuration, 0.1f, 2.0f);

            FindPlayerComponentsIfNull();
        }

        private void OnEnable()
        {
            if (_playerStateEvents != null)
            {
                _playerStateEvents.OnPlayerDeath += HandlePlayerDeath;
            }
        }

        private void OnDisable()
        {
            if (_playerStateEvents != null)
            {
                _playerStateEvents.OnPlayerDeath -= HandlePlayerDeath;
            }
        }

        private void Start()
        {
            FindPlayerComponentsIfNull();
        }

        private void FindPlayerComponentsIfNull()
        {
            if (_playerLocomotion == null)
            {
                _playerLocomotion = FindAnyObjectByType<TacticalLocomotionController>();
            }
            if (_playerLocomotion != null)
            {
                if (_playerHealth == null) _playerHealth = _playerLocomotion.GetComponent<PlayerHealth>();
                if (_playerCombat == null) _playerCombat = _playerLocomotion.GetComponent<PlayerCombatController>();
                if (_playerGadgets == null) _playerGadgets = _playerLocomotion.GetComponent<PlayerGadgetController>();
            }
        }

        public void SetActiveRoom(RoomController room)
        {
            if (room != null)
            {
                _activeRoom = room;
                NullLog.Info("MemoryDump", $"Active room updated to: {room.RoomDisplayName}");
            }
        }

        /// <summary>
        /// Handles player death event and initiates the Memory-Dump Loop (FR-30).
        /// </summary>
        public void HandlePlayerDeath()
        {
            if (_isResetting) return;

            NullLog.Info("MemoryDump", $"Initiating Memory-Dump Loop (UJ-5). Target duration: {_resetDuration:F2}s <= 2.0s.");
            if (_resetRoutine != null)
            {
                StopCoroutine(_resetRoutine);
            }
            _resetRoutine = StartCoroutine(ExecuteMemoryDumpRoutine());
        }

        /// <summary>
        /// Trigger reset manually (e.g. from debug key or test runner).
        /// </summary>
        public void TriggerManualReset()
        {
            HandlePlayerDeath();
        }

        private IEnumerator ExecuteMemoryDumpRoutine()
        {
            _isResetting = true;
            OnMemoryDumpStarted?.Invoke();

            // Glitch dissolve transition
            float elapsed = 0f;
            yield return new WaitForSeconds(_glitchFadeDuration);
            elapsed += _glitchFadeDuration;

            // Wait remaining time before executing the in-place room rollback
            float remaining = Mathf.Max(0f, _resetDuration - elapsed);
            if (remaining > 0f)
            {
                yield return new WaitForSeconds(remaining);
            }

            // Execute instant in-place room reset without scene reloading
            ExecuteInstantReset();

            _isResetting = false;
            OnMemoryDumpCompleted?.Invoke();
            _memoryDumpResetEvents?.RaiseEvent();
            NullLog.Info("MemoryDump", "Memory-Dump Loop complete. Player restored in active room.");
        }

        /// <summary>
        /// Restores room entities, clears transient obstacles, and repositions the player.
        /// </summary>
        public void ExecuteInstantReset()
        {
            if (_activeRoom != null)
            {
                _activeRoom.ResetRoom(_playerLocomotion, _playerHealth);
            }
            else
            {
                // Fallback if no active room set: reset player directly
                if (_playerHealth != null) _playerHealth.ResetState();
                if (_playerLocomotion != null) _playerLocomotion.ResetState();
            }

            // Reset player weapons and gadgets
            if (_playerCombat != null)
            {
                _playerCombat.ResetState();
            }

            if (_playerGadgets != null)
            {
                _playerGadgets.ResetState();
            }
        }
    }
}
