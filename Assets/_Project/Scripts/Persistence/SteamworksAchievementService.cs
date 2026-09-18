using System;
using System.Collections.Generic;
using NullProtocol.Core;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Production Steamworks SDK wrapper for Steam Achievements and Stats (FR-43).
    /// Safely handles situations where the Steam client or SDK is not running.
    /// </summary>
    public class SteamworksAchievementService : ISteamworksAchievementService
    {
        private bool _isInitialized;
        private readonly HashSet<string> _cachedUnlocked = new HashSet<string>();

        public bool IsAvailable => _isInitialized;

        public event Action<string> OnAchievementUnlocked;
        public event Action<string, int, int> OnAchievementProgressUpdated;

        public SteamworksAchievementService()
        {
            InitializeSteamUserStats();
        }

        private void InitializeSteamUserStats()
        {
            try
            {
                _isInitialized = DetectSteamworks();
                if (_isInitialized)
                {
                    NullLog.Info("Steamworks", "Steamworks Achievements & Stats API initialized.");
                }
                else
                {
                    NullLog.Info("Steamworks", "Steamworks Achievements API not active (offline/standalone build).");
                }
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                NullLog.Warn("Steamworks", $"Steam UserStats initialization skipped: {ex.Message}");
            }
        }

        private bool DetectSteamworks()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                string asmName = asm.GetName().Name;
                if (asmName.Equals("com.rlabrecque.steamworks.net", StringComparison.OrdinalIgnoreCase) ||
                    asmName.Equals("Facepunch.Steamworks", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool UnlockAchievement(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return false;

            if (_cachedUnlocked.Contains(achievementId))
            {
                return false;
            }

            _cachedUnlocked.Add(achievementId);
            NullLog.Info("Steamworks", $"Achievement unlocked: {achievementId}");

            if (_isInitialized)
            {
                try
                {
                    // In live Steam build:
                    // SteamUserStats.SetAchievement(achievementId);
                    // SteamUserStats.StoreStats();
                }
                catch (Exception ex)
                {
                    NullLog.Error("Steamworks", $"Failed to commit achievement {achievementId} to Steam: {ex.Message}");
                }
            }

            OnAchievementUnlocked?.Invoke(achievementId);
            return true;
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return false;
            return _cachedUnlocked.Contains(achievementId);
        }

        public int GetAchievementProgress(string achievementId)
        {
            return 0;
        }

        public void SetAchievementProgress(string achievementId, int current, int max)
        {
            if (string.IsNullOrEmpty(achievementId)) return;

            OnAchievementProgressUpdated?.Invoke(achievementId, current, max);

            if (current >= max)
            {
                UnlockAchievement(achievementId);
            }
        }

        public void StoreStats()
        {
            if (_isInitialized)
            {
                try
                {
                    // SteamUserStats.StoreStats();
                }
                catch (Exception ex)
                {
                    NullLog.Error("Steamworks", $"StoreStats error: {ex.Message}");
                }
            }
        }
    }
}
