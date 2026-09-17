using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using NullProtocol.Core;

namespace NullProtocol.World
{
    /// <summary>
    /// Manages campaign progression, level loading, and subsector selection flow (FR-29).
    /// Enforces the 3 distinct campaign levels:
    /// 1. Subsector 01 (Brutalist Corridors / Tactical Orientation)
    /// 2. Subsector 02 (Void Catwalks / Vertical Snipers)
    /// 3. Root Core (Collapsing Arena / Null-01 Duel)
    /// </summary>
    [DisallowMultipleComponent]
    public class CampaignFlowManager : MonoBehaviour
    {
        public static CampaignFlowManager Instance { get; private set; }

        [Header("Campaign Subsectors (FR-29)")]
        [SerializeField] private List<SubsectorData> _subsectors = new List<SubsectorData>();

        [Header("Runtime Progression State")]
        [SerializeField] private SubsectorId _currentSubsectorId = SubsectorId.Subsector01;
        [SerializeField] private bool _autoLoadScenes = false;

        private readonly HashSet<SubsectorId> _unlockedSubsectors = new HashSet<SubsectorId>();

        public SubsectorId CurrentSubsectorId => _currentSubsectorId;
        public IReadOnlyList<SubsectorData> Subsectors => _subsectors;
        public int TotalSubsectorsCount => _subsectors.Count;

        public event Action<SubsectorData> OnSubsectorSelected;
        public event Action<SubsectorId> OnSubsectorUnlocked;
        public event Action<SubsectorData> OnSubsectorCompleted;
        public event Action OnCampaignCompleted;

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

            InitializeDefaultSubsectorsIfEmpty();
            UnlockInitialSubsectors();
        }

        private void InitializeDefaultSubsectorsIfEmpty()
        {
            if (_subsectors == null || _subsectors.Count == 0)
            {
                _subsectors = new List<SubsectorData>
                {
                    SubsectorData.CreateRuntime(
                        SubsectorId.Subsector01,
                        "SEC-01",
                        "Subsector 01",
                        "Brutalist Corridors / Tactical Orientation",
                        "Subsector_01",
                        90.0f,
                        1,
                        true
                    ),
                    SubsectorData.CreateRuntime(
                        SubsectorId.Subsector02,
                        "SEC-02",
                        "Subsector 02",
                        "Void Catwalks / Vertical Snipers",
                        "Subsector_02",
                        150.0f,
                        2,
                        false
                    ),
                    SubsectorData.CreateRuntime(
                        SubsectorId.RootCore,
                        "SEC-03",
                        "Root Core",
                        "Collapsing Arena / Null-01 Duel",
                        "Root_Core",
                        180.0f,
                        1,
                        false
                    )
                };
            }
        }

        private void UnlockInitialSubsectors()
        {
            foreach (var subsector in _subsectors)
            {
                if (subsector.IsUnlockedByDefault)
                {
                    UnlockSubsector(subsector.Id);
                }
            }
            // Subsector 01 is always unlocked by default
            _unlockedSubsectors.Add(SubsectorId.Subsector01);
        }

        public bool IsSubsectorUnlocked(SubsectorId id)
        {
            return _unlockedSubsectors.Contains(id);
        }

        public void UnlockSubsector(SubsectorId id)
        {
            if (!_unlockedSubsectors.Contains(id))
            {
                _unlockedSubsectors.Add(id);
                NullLog.Info("Campaign", $"Unlocked new subsector: {id}");
                OnSubsectorUnlocked?.Invoke(id);
            }
        }

        public SubsectorData GetSubsectorData(SubsectorId id)
        {
            for (int i = 0; i < _subsectors.Count; i++)
            {
                if (_subsectors[i] != null && _subsectors[i].Id == id)
                {
                    return _subsectors[i];
                }
            }
            return null;
        }

        public SubsectorData GetCurrentSubsectorData()
        {
            return GetSubsectorData(_currentSubsectorId);
        }

        /// <summary>
        /// Selects and launches a subsector via the Subsector Select Flow (FR-29).
        /// </summary>
        public bool SelectSubsector(SubsectorId id)
        {
            if (!IsSubsectorUnlocked(id))
            {
                NullLog.Warn("Campaign", $"Cannot select locked subsector: {id}");
                return false;
            }

            _currentSubsectorId = id;
            var data = GetSubsectorData(id);
            NullLog.Info("Campaign", $"Subsector selected: {data?.SubsectorTitle} ({data?.ThemeDescription})");
            OnSubsectorSelected?.Invoke(data);

            if (_autoLoadScenes && data != null && !string.IsNullOrEmpty(data.SceneName))
            {
                SceneManager.LoadScene(data.SceneName);
            }

            return true;
        }

        /// <summary>
        /// Called upon completing the current subsector to unlock the next level and advance progression.
        /// </summary>
        public void CompleteCurrentSubsector()
        {
            var currentData = GetCurrentSubsectorData();
            NullLog.Info("Campaign", $"Completed subsector: {currentData?.SubsectorTitle}");
            OnSubsectorCompleted?.Invoke(currentData);

            // Unlock next sequential subsector
            int nextIndex = (int)_currentSubsectorId + 1;
            if (nextIndex < Enum.GetValues(typeof(SubsectorId)).Length)
            {
                var nextId = (SubsectorId)nextIndex;
                UnlockSubsector(nextId);
            }
            else
            {
                NullLog.Info("Campaign", "Campaign complete! Null-01 eliminated in Root Core.");
                OnCampaignCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Advances directly to the next campaign subsector if unlocked.
        /// </summary>
        public bool AdvanceToNextSubsector()
        {
            int nextIndex = (int)_currentSubsectorId + 1;
            if (nextIndex < Enum.GetValues(typeof(SubsectorId)).Length)
            {
                var nextId = (SubsectorId)nextIndex;
                return SelectSubsector(nextId);
            }
            return false;
        }

        /// <summary>
        /// Resets all campaign progress (locks Subsector 02 and Root Core).
        /// </summary>
        public void ResetProgress()
        {
            _unlockedSubsectors.Clear();
            _currentSubsectorId = SubsectorId.Subsector01;
            UnlockInitialSubsectors();
        }
    }
}
