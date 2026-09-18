using System;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Contract for communicating with the Steamworks Achievements and Stats API (FR-43).
    /// </summary>
    public interface ISteamworksAchievementService
    {
        bool IsAvailable { get; }
        bool UnlockAchievement(string achievementId);
        bool IsAchievementUnlocked(string achievementId);
        int GetAchievementProgress(string achievementId);
        void SetAchievementProgress(string achievementId, int current, int max);
        void StoreStats();

        event Action<string> OnAchievementUnlocked;
        event Action<string, int, int> OnAchievementProgressUpdated;
    }
}
