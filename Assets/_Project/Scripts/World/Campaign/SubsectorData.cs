using System;
using UnityEngine;

namespace NullProtocol.World
{
    public enum SubsectorId
    {
        Subsector01 = 0,
        Subsector02 = 1,
        RootCore = 2
    }

    /// <summary>
    /// Configuration data for a campaign subsector level (FR-29).
    /// </summary>
    [CreateAssetMenu(fileName = "NewSubsectorData", menuName = "NullProtocol/Campaign/Subsector Data")]
    public class SubsectorData : ScriptableObject
    {
        [Header("Identity (FR-29)")]
        [SerializeField] private SubsectorId _subsectorId;
        [SerializeField] private string _subsectorCode = "SEC-01";
        [SerializeField] private string _subsectorTitle = "Subsector 01";
        [SerializeField] private string _themeDescription = "Brutalist Corridors / Tactical Orientation";
        [SerializeField] private string _sceneName = "Subsector_01";

        [Header("Tactical Constraints & Objectives (FR-28, FR-32)")]
        [SerializeField] private float _maxEngagementDistance = ModularGrid.MAX_SIGHTLINE_DISTANCE; // 12m
        [SerializeField] private float _parTimeSeconds = 90.0f;
        [SerializeField] private int _requiredDataCores = 1;

        [Header("Progression Status")]
        [SerializeField] private bool _isUnlockedByDefault = false;

        public SubsectorId Id => _subsectorId;
        public string SubsectorCode => _subsectorCode;
        public string SubsectorTitle => _subsectorTitle;
        public string ThemeDescription => _themeDescription;
        public string SceneName => _sceneName;
        public float MaxEngagementDistance => _maxEngagementDistance;
        public float ParTimeSeconds => _parTimeSeconds;
        public int RequiredDataCores => _requiredDataCores;
        public bool IsUnlockedByDefault => _isUnlockedByDefault;

        public static SubsectorData CreateRuntime(
            SubsectorId id,
            string code,
            string title,
            string theme,
            string scene,
            float parTime,
            int requiredCores,
            bool unlockedByDefault)
        {
            var data = CreateInstance<SubsectorData>();
            data._subsectorId = id;
            data._subsectorCode = code;
            data._subsectorTitle = title;
            data._themeDescription = theme;
            data._sceneName = scene;
            data._parTimeSeconds = parTime;
            data._requiredDataCores = requiredCores;
            data._isUnlockedByDefault = unlockedByDefault;
            return data;
        }
    }
}
