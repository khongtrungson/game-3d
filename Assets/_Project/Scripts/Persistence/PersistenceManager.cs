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
    /// Master orchestrator for session persistence, profile storage, Steam Cloud sync,
    /// and achievement tracking (FR-40, FR-41, FR-42, FR-43).
    /// </summary>
    [DisallowMultipleComponent]
    public class PersistenceManager : MonoBehaviour
    {
        public static PersistenceManager Instance { get; private set; }

        [Header("Event Channel Listeners")]
        [SerializeField] private DamageEventChannelSO _damageChannel;
        [SerializeField] private KillEventChannelSO _killChannel;
        [SerializeField] private GadgetEventChannelSO _gadgetChannel;
        [SerializeField] private PlayerStateEventChannelSO _playerStateEvents;

        [Header("Cloud Sync Configuration")]
        [SerializeField] private bool _autoSyncCloudOnAwake = true;
        [SerializeField] private bool _autoSyncCloudOnSave = true;

        // Core Persistence Subsystems
        private IProfileStorageService _storageService;
        private ISteamCloudStorage _cloudStorage;
        private SteamCloudSyncManager _cloudSyncManager;
        private ISteamworksAchievementService _steamworksAchievementService;
        private AchievementTracker _achievementTracker;
        private CheckpointManager _checkpointManager;
        private ProfileData _currentProfile;

        public ProfileData CurrentProfile => _currentProfile;
        public IProfileStorageService StorageService => _storageService;
        public ISteamCloudStorage CloudStorage => _cloudStorage;
        public SteamCloudSyncManager CloudSyncManager => _cloudSyncManager;
        public ISteamworksAchievementService SteamworksService => _steamworksAchievementService;
        public AchievementTracker Achievements => _achievementTracker;
        public CheckpointManager Checkpoints => _checkpointManager;

        public event Action<ProfileData> OnProfileUpdated;
        public event Action<AchievementDefinition> OnAchievementUnlocked;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeServices();
        }

        public void InitializeServices(
            IProfileStorageService customStorage = null,
            ISteamCloudStorage customCloud = null,
            ISteamworksAchievementService customAchievements = null)
        {
            _storageService = customStorage ?? new ProfileStorageService();
            _cloudStorage = customCloud ?? new SteamCloudStorageService();
            _cloudSyncManager = new SteamCloudSyncManager(_storageService, _cloudStorage);
            _steamworksAchievementService = customAchievements ?? new SteamworksAchievementService();

            if (_autoSyncCloudOnAwake && _cloudStorage.IsAvailable)
            {
                _cloudSyncManager.Synchronize();
            }

            _currentProfile = _storageService.Load();

            _achievementTracker = new AchievementTracker(_currentProfile, _steamworksAchievementService);
            _checkpointManager = new CheckpointManager(_currentProfile, _storageService);

            _achievementTracker.OnAchievementUnlocked += HandleAchievementUnlocked;

            // Wire SettingsManager hooks (FR-44, FR-45, FR-46)
            SettingsManager.ProfileSettingsLoader = () => _currentProfile?.Settings;
            SettingsManager.ProfileSettingsSaver = settings =>
            {
                if (_currentProfile != null)
                {
                    _currentProfile.Settings = settings;
                    SaveProfile();
                }
            };
        }

        private void OnEnable()
        {
            if (_damageChannel != null)
            {
                _damageChannel.OnEventRaised += HandleDamageDealt;
            }

            if (_killChannel != null)
            {
                _killChannel.OnKillRegistered += HandleKill;
            }

            if (_gadgetChannel != null)
            {
                _gadgetChannel.OnGadgetUsed += HandleGadgetUsed;
            }

            if (_playerStateEvents != null)
            {
                _playerStateEvents.OnHealthChanged += HandlePlayerHealthChanged;
            }
        }

        private void OnDisable()
        {
            if (_damageChannel != null)
            {
                _damageChannel.OnEventRaised -= HandleDamageDealt;
            }

            if (_killChannel != null)
            {
                _killChannel.OnKillRegistered -= HandleKill;
            }

            if (_gadgetChannel != null)
            {
                _gadgetChannel.OnGadgetUsed -= HandleGadgetUsed;
            }

            if (_playerStateEvents != null)
            {
                _playerStateEvents.OnHealthChanged -= HandlePlayerHealthChanged;
            }

            if (_achievementTracker != null)
            {
                _achievementTracker.OnAchievementUnlocked -= HandleAchievementUnlocked;
            }
        }

        #region Event Callbacks & Routing

        private void HandleDamageDealt(int damage, Vector3 hitPoint, bool isHeadshot)
        {
            _currentProfile.Metrics.TotalShotsHit++;
            if (isHeadshot)
            {
                _currentProfile.Metrics.TotalHeadshots++;
            }
        }

        private void HandleKill(IDamageable victim, bool isHeadshot, Vector3 hitPoint)
        {
            _currentProfile.Metrics.TotalEnemiesEliminated++;
        }

        private void HandleGadgetUsed(GadgetType type, int remainingCharges)
        {
            _achievementTracker?.HandleGadgetUsed(type, remainingCharges);
        }

        private void HandlePlayerHealthChanged(int currentHp, int maxHp)
        {
            // Track damage for active room
            // Handled when player takes damage
        }

        private void HandleAchievementUnlocked(AchievementDefinition achievement)
        {
            SaveProfile();
            OnAchievementUnlocked?.Invoke(achievement);
        }

        #endregion

        #region Room & Checkpoint Triggers (FR-41)

        /// <summary>
        /// Called when the player enters a room threshold (FR-41).
        /// If the room is uncleared, automatically commits checkpoint to disk.
        /// </summary>
        public void OnPlayerEnteredRoom(
            RoomController room,
            SubsectorId currentSubsector,
            PlayerHealth playerHealth = null,
            PlayerGadgetController playerGadgets = null)
        {
            _achievementTracker?.HandleRoomEntered(room);

            if (_checkpointManager != null && room != null && !room.IsCleared)
            {
                _checkpointManager.TryCommitRoomCheckpoint(room, currentSubsector, playerHealth, playerGadgets);

                if (_autoSyncCloudOnSave)
                {
                    _cloudSyncManager?.PushToCloud();
                }
            }
        }

        /// <summary>
        /// Called when a combat room is cleared of all hostiles.
        /// Evaluates Clean Sweep achievement.
        /// </summary>
        public void OnPlayerClearedRoom(RoomController room)
        {
            _achievementTracker?.HandleRoomCleared(room);
        }

        /// <summary>
        /// Called when level completion debrief is generated (FR-41, FR-43).
        /// Evaluates debrief achievements and commits progression state to disk.
        /// </summary>
        public void OnLevelCompleted(SubsectorId completedSubsector, TacticalDebriefData debrief)
        {
            _achievementTracker?.EvaluateDebriefAchievements(debrief);
            _checkpointManager?.CommitLevelCompletion(completedSubsector, debrief);

            if (_autoSyncCloudOnSave)
            {
                _cloudSyncManager?.PushToCloud();
            }

            OnProfileUpdated?.Invoke(_currentProfile);
        }

        #endregion

        #region Profile Operations

        /// <summary>
        /// Saves current profile to disk (FR-40) and pushes to Steam Cloud if enabled (FR-42).
        /// </summary>
        public bool SaveProfile()
        {
            if (_storageService == null || _currentProfile == null)
            {
                return false;
            }

            bool success = _storageService.Save(_currentProfile);
            if (success && _autoSyncCloudOnSave)
            {
                _cloudSyncManager?.PushToCloud();
            }

            OnProfileUpdated?.Invoke(_currentProfile);
            return success;
        }

        /// <summary>
        /// Resets campaign progress while preserving lifetime metrics and achievements.
        /// </summary>
        public void ResetCampaignProgress()
        {
            _currentProfile.CurrentSubsector = SubsectorId.Subsector01;
            _currentProfile.UnlockedSubsectors.Clear();
            _currentProfile.UnlockedSubsectors.Add(SubsectorId.Subsector01);
            _currentProfile.ActiveCheckpoint.Clear();

            SaveProfile();
        }

        /// <summary>
        /// Deletes profile from local disk and creates a clean default profile.
        /// </summary>
        public void ClearEntireProfile()
        {
            _storageService?.Delete();
            _currentProfile = new ProfileData();
            _achievementTracker = new AchievementTracker(_currentProfile, _steamworksAchievementService);
            _checkpointManager = new CheckpointManager(_currentProfile, _storageService);

            SaveProfile();
        }

        #endregion
    }
}
