using System;

namespace NullProtocol.Persistence
{
    public enum AchievementCategory
    {
        CampaignMilestone,
        TacticalMastery,
        GadgetProficiency
    }

    /// <summary>
    /// Definition of a Steam Achievement (FR-43).
    /// </summary>
    [Serializable]
    public class AchievementDefinition
    {
        public string Id;
        public string Title;
        public string Description;
        public AchievementCategory Category;
        public bool IsProgressStat;
        public int TargetProgress;

        public AchievementDefinition(
            string id,
            string title,
            string description,
            AchievementCategory category,
            bool isProgressStat = false,
            int targetProgress = 1)
        {
            Id = id;
            Title = title;
            Description = description;
            Category = category;
            IsProgressStat = isProgressStat;
            TargetProgress = targetProgress;
        }
    }
}
