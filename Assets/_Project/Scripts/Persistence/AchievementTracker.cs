using System;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.UI;
using NullProtocol.World;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Evaluates gameplay events against the 10 core Steam Achievements and commits unlock states (FR-43).
    /// Tracks campaign milestones, tactical mastery (Clean Sweep, Surgical Execution), and gadget proficiency.
    /// </summary>
    public class AchievementTracker
    {
        private readonly ProfileData _profile;
        private readonly ISteamworksAchievementService _steamworksService;

        // Per-room tracking for Clean Sweep (zero-damage room clears)
        private int _currentRoomDamageTaken;
        private string _activeRoomId = string.Empty;

        public event Action<AchievementDefinition> OnAchievementUnlocked;

        public AchievementTracker(ProfileData profile, ISteamworksAchievementService steamworksService)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _steamworksService = steamworksService ?? throw new ArgumentNullException(nameof(steamworksService));
        }

        #region Campaign Milestones & Debrief Evaluation (FR-43)

        /// <summary>
        /// Evaluates level completion debrief metrics for campaign milestone and tactical mastery achievements (FR-43).
        /// </summary>
        public void EvaluateDebriefAchievements(TacticalDebriefData debrief)
        {
            // 1. Campaign Milestones
            switch (debrief.Subsector)
            {
                case SubsectorId.Subsector01:
                    TryUnlock(AchievementDatabase.ACH_SUBSECTOR_01_CLEAR);
                    break;
                case SubsectorId.Subsector02:
                    TryUnlock(AchievementDatabase.ACH_SUBSECTOR_02_CLEAR);
                    break;
                case SubsectorId.RootCore:
                    TryUnlock(AchievementDatabase.ACH_ROOT_CORE_CLEAR);
                    break;
            }

            // 2. Tactical Mastery: Untouchable (zero damage across entire level)
            if (debrief.DamageTaken == 0)
            {
                TryUnlock(AchievementDatabase.ACH_UNTOUCHABLE);
            }

            // 3. Tactical Mastery: Ghost in the System (Grade S)
            if (debrief.Grade == TacticalGrade.S)
            {
                TryUnlock(AchievementDatabase.ACH_GHOST_CLEAR);
            }

            // 4. Tactical Mastery: Surgical Execution (80%+ headshot rate or 80%+ accuracy with headshots)
            float headshotRate = debrief.HeadshotPercentage;
            if (headshotRate >= 80.0f || (debrief.AccuracyPercentage >= 80.0f && debrief.HeadshotsCount > 0))
            {
                TryUnlock(AchievementDatabase.ACH_SURGICAL_EXECUTION);
            }

            // Update lifetime metrics
            _profile.Metrics.TotalShotsFired += debrief.ShotsFired;
            _profile.Metrics.TotalShotsHit += debrief.ShotsHit;
            _profile.Metrics.TotalHeadshots += debrief.HeadshotsCount;
            _profile.Metrics.TotalDamageTaken += debrief.DamageTaken;
            _profile.Metrics.TotalEnemiesEliminated += debrief.EnemiesNeutralized;
            _profile.Metrics.TotalPlayTimeSeconds += debrief.ElapsedTimeSeconds;
        }

        #endregion

        #region Tactical Mastery: Clean Sweep (FR-43)

        public void HandleRoomEntered(RoomController room)
        {
            if (room == null) return;
            _activeRoomId = room.RoomId;
            _currentRoomDamageTaken = 0;
        }

        public void HandlePlayerDamaged(int damageAmount)
        {
            if (damageAmount > 0)
            {
                _currentRoomDamageTaken += damageAmount;
            }
        }

        public void HandleRoomCleared(RoomController room)
        {
            if (room == null) return;

            _profile.Metrics.TotalRoomsCleared++;

            // Clean Sweep: Clear any combat room taking zero damage (FR-43)
            if (_currentRoomDamageTaken == 0)
            {
                _profile.Metrics.TotalZeroDamageRoomsCleared++;
                TryUnlock(AchievementDatabase.ACH_CLEAN_SWEEP);
            }
        }

        #endregion

        #region Gadget Proficiency (FR-43)

        public void HandleGadgetUsed(GadgetType gadgetType, int remainingCharges)
        {
            switch (gadgetType)
            {
                case GadgetType.HardLightBarricade:
                    _profile.Metrics.TotalBarricadesDeployed++;
                    _steamworksService.SetAchievementProgress(
                        AchievementDatabase.ACH_GADGET_BARRICADE,
                        _profile.Metrics.TotalBarricadesDeployed,
                        10
                    );
                    if (_profile.Metrics.TotalBarricadesDeployed >= 10)
                    {
                        TryUnlock(AchievementDatabase.ACH_GADGET_BARRICADE);
                    }
                    break;

                case GadgetType.NullCloudSmoke:
                    _profile.Metrics.TotalSmokesDeployed++;
                    _steamworksService.SetAchievementProgress(
                        AchievementDatabase.ACH_GADGET_SMOKE,
                        _profile.Metrics.TotalSmokesDeployed,
                        10
                    );
                    if (_profile.Metrics.TotalSmokesDeployed >= 10)
                    {
                        TryUnlock(AchievementDatabase.ACH_GADGET_SMOKE);
                    }
                    break;
            }
        }

        public void HandleHostileFrozenByMine()
        {
            _profile.Metrics.TotalTripMinesFrozen++;
            _steamworksService.SetAchievementProgress(
                AchievementDatabase.ACH_GADGET_TRIPMINE,
                _profile.Metrics.TotalTripMinesFrozen,
                5
            );

            if (_profile.Metrics.TotalTripMinesFrozen >= 5)
            {
                TryUnlock(AchievementDatabase.ACH_GADGET_TRIPMINE);
            }
        }

        #endregion

        public bool TryUnlock(string achievementId)
        {
            var def = AchievementDatabase.Get(achievementId);
            if (def == null)
            {
                NullLog.Warn("Achievements", $"Unknown achievement ID: {achievementId}");
                return false;
            }

            if (_profile.UnlockAchievement(achievementId))
            {
                _steamworksService.UnlockAchievement(achievementId);
                _steamworksService.StoreStats();

                NullLog.Info("Achievements", $"ACHIEVEMENT UNLOCKED: [{def.Title}] - {def.Description} (FR-43)");
                OnAchievementUnlocked?.Invoke(def);
                return true;
            }

            return false;
        }

        public bool IsUnlocked(string achievementId)
        {
            return _profile.IsAchievementUnlocked(achievementId);
        }
    }
}
