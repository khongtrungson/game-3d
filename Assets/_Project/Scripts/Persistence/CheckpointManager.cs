using System;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.Controller;
using NullProtocol.Gadgets;
using NullProtocol.UI;
using NullProtocol.World;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Coordinates automatic checkpoint commits to disk upon entering uncleared rooms
    /// and upon level completion (FR-41).
    /// </summary>
    public class CheckpointManager
    {
        private readonly ProfileData _profile;
        private readonly IProfileStorageService _storageService;

        public event Action<CheckpointData> OnCheckpointCommitted;
        public event Action<SubsectorId, TacticalDebriefData> OnLevelCompletionCommitted;

        public bool HasActiveCheckpoint => _profile?.ActiveCheckpoint != null && _profile.ActiveCheckpoint.HasActiveCheckpoint;
        public CheckpointData ActiveCheckpoint => _profile?.ActiveCheckpoint;

        public CheckpointManager(ProfileData profile, IProfileStorageService storageService)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        /// <summary>
        /// Captures and commits checkpoint state to disk upon entering an uncleared room (FR-41).
        /// If the room is already cleared, checkpoint creation is skipped.
        /// </summary>
        public bool TryCommitRoomCheckpoint(
            RoomController room,
            SubsectorId currentSubsector = SubsectorId.Subsector01,
            PlayerHealth playerHealth = null,
            PlayerGadgetController playerGadgets = null)
        {
            if (room == null)
            {
                return false;
            }

            // FR-41: Only commit upon entering ANY UNCLEARED room
            if (room.IsCleared)
            {
                NullLog.Info("Checkpoint", $"Skipping checkpoint commit: room '{room.RoomDisplayName}' is already cleared.");
                return false;
            }

            int health = (playerHealth != null) ? playerHealth.CurrentHealth : 100;
            int maxHealth = (playerHealth != null) ? playerHealth.MaxHealth : 100;
            int barricades = (playerGadgets != null) ? playerGadgets.BarricadeCharges : 2;
            int smokes = (playerGadgets != null) ? playerGadgets.SmokeCharges : 2;
            int tripMines = (playerGadgets != null) ? playerGadgets.TripMineCharges : 1;

            Vector3 spawnPos = (room.SpawnPoint != null) ? room.SpawnPoint.position : room.transform.position;
            Quaternion spawnRot = (room.SpawnPoint != null) ? room.SpawnPoint.rotation : room.transform.rotation;

            var checkpoint = new CheckpointData(
                currentSubsector,
                room.RoomId,
                room.RoomDisplayName,
                health,
                maxHealth,
                barricades,
                smokes,
                tripMines,
                spawnPos,
                spawnRot
            );

            _profile.ActiveCheckpoint = checkpoint;
            _profile.CurrentSubsector = currentSubsector;

            // Commit to disk immediately (FR-41)
            bool saved = _storageService.Save(_profile);

            NullLog.Info("Checkpoint", $"Committed checkpoint to disk for room '{room.RoomDisplayName}' in {currentSubsector} (FR-41). Success: {saved}");
            OnCheckpointCommitted?.Invoke(checkpoint);
            return saved;
        }

        /// <summary>
        /// Commits level progression and tactical debrief stats to disk upon level completion (FR-41).
        /// Clears active mid-level checkpoint and unlocks next sequential subsector.
        /// </summary>
        public bool CommitLevelCompletion(SubsectorId completedSubsector, TacticalDebriefData debrief)
        {
            _profile.EnsureSubsectorRecords();

            var record = _profile.GetRecord(completedSubsector);
            if (record != null)
            {
                record.RecordCompletion(debrief);
            }

            // Unlock next sequential subsector
            int nextIndex = (int)completedSubsector + 1;
            if (nextIndex < Enum.GetValues(typeof(SubsectorId)).Length)
            {
                var nextSubsector = (SubsectorId)nextIndex;
                _profile.UnlockSubsector(nextSubsector);
                _profile.CurrentSubsector = nextSubsector;
            }

            // Clear active mid-level checkpoint upon level completion
            _profile.ActiveCheckpoint.Clear();

            // Commit to disk immediately (FR-41)
            bool saved = _storageService.Save(_profile);

            NullLog.Info("Checkpoint", $"Level completion committed to disk for {completedSubsector} with Grade {debrief.Grade} (FR-41). Success: {saved}");
            OnLevelCompletionCommitted?.Invoke(completedSubsector, debrief);
            return saved;
        }

        /// <summary>
        /// Restores player state and position from the active checkpoint (FR-41).
        /// </summary>
        public bool RestoreCheckpoint(
            TacticalLocomotionController playerLocomotion = null,
            PlayerHealth playerHealth = null)
        {
            if (!HasActiveCheckpoint)
            {
                NullLog.Warn("Checkpoint", "No active checkpoint available to restore.");
                return false;
            }

            var cp = _profile.ActiveCheckpoint;

            if (playerLocomotion != null)
            {
                var charController = playerLocomotion.GetComponent<CharacterController>();
                if (charController != null) charController.enabled = false;

                playerLocomotion.transform.SetPositionAndRotation(cp.PlayerPosition, cp.PlayerRotation);

                if (charController != null) charController.enabled = true;
            }

            if (playerHealth != null)
            {
                playerHealth.ResetState();
            }

            NullLog.Info("Checkpoint", $"Restored checkpoint at room '{cp.RoomDisplayName}' ({cp.RoomId})");
            return true;
        }
    }
}
