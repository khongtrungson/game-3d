using System;
using System.Collections.Generic;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Mock implementation of ISteamworksAchievementService for unit testing and offline execution (FR-43).
    /// </summary>
    public class MockSteamworksAchievementService : ISteamworksAchievementService
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();
        private readonly Dictionary<string, int> _progress = new Dictionary<string, int>();
        private bool _isAvailable = true;

        public bool IsAvailable
        {
            get => _isAvailable;
            set => _isAvailable = value;
        }

        public IReadOnlyCollection<string> UnlockedAchievements => _unlocked;

        public event Action<string> OnAchievementUnlocked;
        public event Action<string, int, int> OnAchievementProgressUpdated;

        public bool UnlockAchievement(string achievementId)
        {
            if (!_isAvailable || string.IsNullOrEmpty(achievementId))
            {
                return false;
            }

            if (!_unlocked.Contains(achievementId))
            {
                _unlocked.Add(achievementId);
                OnAchievementUnlocked?.Invoke(achievementId);
                return true;
            }

            return false;
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            return _unlocked.Contains(achievementId);
        }

        public int GetAchievementProgress(string achievementId)
        {
            if (_progress.TryGetValue(achievementId, out int val))
            {
                return val;
            }
            return 0;
        }

        public void SetAchievementProgress(string achievementId, int current, int max)
        {
            if (!_isAvailable || string.IsNullOrEmpty(achievementId)) return;

            _progress[achievementId] = current;
            OnAchievementProgressUpdated?.Invoke(achievementId, current, max);

            if (current >= max && !_unlocked.Contains(achievementId))
            {
                UnlockAchievement(achievementId);
            }
        }

        public void StoreStats()
        {
            // No-op in mock
        }

        public void Clear()
        {
            _unlocked.Clear();
            _progress.Clear();
        }
    }
}
