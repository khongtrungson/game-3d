using System.Collections.Generic;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Master registry of the 10 core Steam Achievements covering campaign milestones,
    /// tactical mastery, and gadget proficiency (FR-43).
    /// </summary>
    public static class AchievementDatabase
    {
        // 1. Campaign Milestones
        public const string ACH_SUBSECTOR_01_CLEAR = "ACH_SUBSECTOR_01_CLEAR";
        public const string ACH_SUBSECTOR_02_CLEAR = "ACH_SUBSECTOR_02_CLEAR";
        public const string ACH_ROOT_CORE_CLEAR = "ACH_ROOT_CORE_CLEAR";

        // 2. Tactical Mastery
        public const string ACH_CLEAN_SWEEP = "ACH_CLEAN_SWEEP";
        public const string ACH_SURGICAL_EXECUTION = "ACH_SURGICAL_EXECUTION";
        public const string ACH_GHOST_CLEAR = "ACH_GHOST_CLEAR";
        public const string ACH_UNTOUCHABLE = "ACH_UNTOUCHABLE";

        // 3. Gadget Proficiency
        public const string ACH_GADGET_BARRICADE = "ACH_GADGET_BARRICADE";
        public const string ACH_GADGET_SMOKE = "ACH_GADGET_SMOKE";
        public const string ACH_GADGET_TRIPMINE = "ACH_GADGET_TRIPMINE";

        private static readonly Dictionary<string, AchievementDefinition> _registry = new Dictionary<string, AchievementDefinition>
        {
            // Campaign Milestones
            {
                ACH_SUBSECTOR_01_CLEAR,
                new AchievementDefinition(
                    ACH_SUBSECTOR_01_CLEAR,
                    "Subsector Purged",
                    "Complete Subsector 01 and successfully extract from the memory leaks.",
                    AchievementCategory.CampaignMilestone
                )
            },
            {
                ACH_SUBSECTOR_02_CLEAR,
                new AchievementDefinition(
                    ACH_SUBSECTOR_02_CLEAR,
                    "Archive Purged",
                    "Complete Subsector 02 across the vertical void catwalks.",
                    AchievementCategory.CampaignMilestone
                )
            },
            {
                ACH_ROOT_CORE_CLEAR,
                new AchievementDefinition(
                    ACH_ROOT_CORE_CLEAR,
                    "Root Core Dereferenced",
                    "Defeat Null-01 and purge the collapsing Root Core simulation.",
                    AchievementCategory.CampaignMilestone
                )
            },

            // Tactical Mastery
            {
                ACH_CLEAN_SWEEP,
                new AchievementDefinition(
                    ACH_CLEAN_SWEEP,
                    "Clean Sweep",
                    "Neutralize all hostiles and clear any combat room taking zero damage.",
                    AchievementCategory.TacticalMastery
                )
            },
            {
                ACH_SURGICAL_EXECUTION,
                new AchievementDefinition(
                    ACH_SURGICAL_EXECUTION,
                    "Surgical Execution",
                    "Complete a level with an 80%+ headshot rate.",
                    AchievementCategory.TacticalMastery
                )
            },
            {
                ACH_GHOST_CLEAR,
                new AchievementDefinition(
                    ACH_GHOST_CLEAR,
                    "Ghost in the System",
                    "Earn a Grade S tactical debrief rating on any subsector.",
                    AchievementCategory.TacticalMastery
                )
            },
            {
                ACH_UNTOUCHABLE,
                new AchievementDefinition(
                    ACH_UNTOUCHABLE,
                    "Untouchable",
                    "Complete an entire subsector campaign run taking zero total damage.",
                    AchievementCategory.TacticalMastery
                )
            },

            // Gadget Proficiency
            {
                ACH_GADGET_BARRICADE,
                new AchievementDefinition(
                    ACH_GADGET_BARRICADE,
                    "Hard-Light Architect",
                    "Deploy 10 Hard-Light Barricades to shape dynamic cover.",
                    AchievementCategory.GadgetProficiency,
                    isProgressStat: true,
                    targetProgress: 10
                )
            },
            {
                ACH_GADGET_SMOKE,
                new AchievementDefinition(
                    ACH_GADGET_SMOKE,
                    "Sensor Blind",
                    "Deploy 10 Null-Cloud Smoke Canisters to scramble hostile line of sight.",
                    AchievementCategory.GadgetProficiency,
                    isProgressStat: true,
                    targetProgress: 10
                )
            },
            {
                ACH_GADGET_TRIPMINE,
                new AchievementDefinition(
                    ACH_GADGET_TRIPMINE,
                    "Logic Frozen",
                    "Freeze 5 hostiles into wireframe states using Logic-Trip Mines.",
                    AchievementCategory.GadgetProficiency,
                    isProgressStat: true,
                    targetProgress: 5
                )
            }
        };

        public static IReadOnlyDictionary<string, AchievementDefinition> AllAchievements => _registry;
        public static int TotalAchievementsCount => _registry.Count;

        public static AchievementDefinition Get(string id)
        {
            if (_registry.TryGetValue(id, out var def))
            {
                return def;
            }
            return null;
        }

        public static bool Contains(string id)
        {
            return _registry.ContainsKey(id);
        }
    }
}
