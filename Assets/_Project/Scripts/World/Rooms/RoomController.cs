using System;
using System.Collections.Generic;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.Controller;

namespace NullProtocol.World
{
    /// <summary>
    /// Coordinates room-level state, volatile entities, and instant reset execution (FR-30, UJ-5).
    /// </summary>
    [DisallowMultipleComponent]
    public class RoomController : MonoBehaviour, IResettable
    {
        [Header("Room Identity")]
        [SerializeField] private string _roomId = "Room_01";
        [SerializeField] private string _roomDisplayName = "Corridor 01";

        [Header("Spawn & Entrance Configuration (FR-30)")]
        [Tooltip("Transform where player respawns on room memory-dump reset")]
        [SerializeField] private Transform _spawnPoint;

        [Header("Room Dimensions (FR-28: 4m Grid)")]
        [SerializeField] private Bounds _roomBounds = new Bounds(Vector3.zero, new Vector3(12f, 4f, 12f));

        private readonly List<IResettable> _roomResettables = new List<IResettable>();
        private bool _isPlayerInside;
        private bool _isCleared;

        public string RoomId => _roomId;
        public string RoomDisplayName => _roomDisplayName;
        public Transform SpawnPoint => _spawnPoint;
        public Bounds RoomBounds => _roomBounds;
        public bool IsPlayerInside => _isPlayerInside;
        public bool IsCleared => _isCleared;
        public IReadOnlyList<IResettable> Resettables => _roomResettables;

        public event Action<RoomController> OnRoomEntered;
        public event Action<RoomController> OnRoomReset;
        public event Action<RoomController> OnRoomCleared;

        private void Awake()
        {
            if (_spawnPoint == null)
            {
                // Fallback to this transform if spawn point not assigned
                _spawnPoint = transform;
            }

            // Auto-register any IResettable children initially placed in the room
            var initialResettables = GetComponentsInChildren<IResettable>(true);
            foreach (var r in initialResettables)
            {
                if (r != (IResettable)this)
                {
                    RegisterResettable(r);
                }
            }
        }

        public void RegisterResettable(IResettable resettable)
        {
            if (resettable != null && !_roomResettables.Contains(resettable))
            {
                _roomResettables.Add(resettable);
            }
        }

        public void UnregisterResettable(IResettable resettable)
        {
            if (resettable != null)
            {
                _roomResettables.Remove(resettable);
            }
        }

        /// <summary>
        /// Called when the player crosses this room's threshold trigger.
        /// </summary>
        public void HandlePlayerEnter()
        {
            _isPlayerInside = true;
            NullLog.Info("Room", $"Player entered room: {_roomDisplayName} ({_roomId})");
            OnRoomEntered?.Invoke(this);
        }

        /// <summary>
        /// Called when the player leaves this room.
        /// </summary>
        public void HandlePlayerExit()
        {
            _isPlayerInside = false;
        }

        /// <summary>
        /// Marks the room encounter as successfully cleared.
        /// </summary>
        public void SetRoomCleared()
        {
            if (!_isCleared)
            {
                _isCleared = true;
                NullLog.Info("Room", $"Room cleared: {_roomDisplayName}");
                OnRoomCleared?.Invoke(this);
            }
        }

        /// <summary>
        /// Executes room-level state rollback and entity re-initialization (FR-30, UJ-5).
        /// </summary>
        public void ResetRoom(TacticalLocomotionController playerLocomotion = null, PlayerHealth playerHealth = null)
        {
            NullLog.Info("Room", $"Executing instantaneous room reset for: {_roomDisplayName}");

            // 1. Reset all registered volatile room actors (enemies, deployed barricades, mines, etc.)
            for (int i = 0; i < _roomResettables.Count; i++)
            {
                var resettable = _roomResettables[i];
                if (resettable != null)
                {
                    resettable.ResetState();
                }
            }

            // 2. Reposition player safely to room entrance spawn point
            if (playerLocomotion != null && _spawnPoint != null)
            {
                var charController = playerLocomotion.GetComponent<CharacterController>();
                if (charController != null) charController.enabled = false;

                playerLocomotion.transform.SetPositionAndRotation(_spawnPoint.position, _spawnPoint.rotation);
                playerLocomotion.ResetState();

                if (charController != null) charController.enabled = true;
            }

            // 3. Reset player health
            if (playerHealth != null)
            {
                playerHealth.ResetState();
            }

            OnRoomReset?.Invoke(this);
        }

        public void ResetState()
        {
            ResetRoom(null, null);
        }
    }
}
