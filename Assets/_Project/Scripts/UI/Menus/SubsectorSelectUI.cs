using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NullProtocol.Core;
using NullProtocol.World;

namespace NullProtocol.UI
{
    /// <summary>
    /// UI menu for the Subsector Select Flow (FR-29).
    /// Displays Subsector 01, Subsector 02, and Root Core with locked/unlocked statuses.
    /// </summary>
    [DisallowMultipleComponent]
    public class SubsectorSelectUI : MonoBehaviour
    {
        [Header("Menu Root")]
        [SerializeField] private GameObject _menuPanel;

        [Header("Subsector Buttons")]
        [SerializeField] private Button _subsector01Button;
        [SerializeField] private Button _subsector02Button;
        [SerializeField] private Button _rootCoreButton;

        [Header("Status Labels (Optional)")]
        [SerializeField] private TextMeshProUGUI _subsector01Status;
        [SerializeField] private TextMeshProUGUI _subsector02Status;
        [SerializeField] private TextMeshProUGUI _rootCoreStatus;

        public event Action<SubsectorId> OnSubsectorChosen;

        private void Awake()
        {
            if (_menuPanel == null)
            {
                _menuPanel = gameObject;
            }

            HookButtons();
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void HookButtons()
        {
            if (_subsector01Button != null)
            {
                _subsector01Button.onClick.AddListener(() => ChooseSubsector(SubsectorId.Subsector01));
            }

            if (_subsector02Button != null)
            {
                _subsector02Button.onClick.AddListener(() => ChooseSubsector(SubsectorId.Subsector02));
            }

            if (_rootCoreButton != null)
            {
                _rootCoreButton.onClick.AddListener(() => ChooseSubsector(SubsectorId.RootCore));
            }
        }

        /// <summary>
        /// Updates the interactability and labels of each subsector card based on progression.
        /// </summary>
        public void RefreshUI()
        {
            var manager = CampaignFlowManager.Instance;
            if (manager == null) return;

            bool s1Unlocked = manager.IsSubsectorUnlocked(SubsectorId.Subsector01);
            bool s2Unlocked = manager.IsSubsectorUnlocked(SubsectorId.Subsector02);
            bool rootUnlocked = manager.IsSubsectorUnlocked(SubsectorId.RootCore);

            if (_subsector01Button != null) _subsector01Button.interactable = s1Unlocked;
            if (_subsector02Button != null) _subsector02Button.interactable = s2Unlocked;
            if (_rootCoreButton != null) _rootCoreButton.interactable = rootUnlocked;

            if (_subsector01Status != null) _subsector01Status.text = s1Unlocked ? "UNLOCKED // READY" : "LOCKED";
            if (_subsector02Status != null) _subsector02Status.text = s2Unlocked ? "UNLOCKED // READY" : "LOCKED [CLEAR SUBSECTOR 01]";
            if (_rootCoreStatus != null) _rootCoreStatus.text = rootUnlocked ? "UNLOCKED // READY" : "LOCKED [CLEAR SUBSECTOR 02]";
        }

        public void ChooseSubsector(SubsectorId subsectorId)
        {
            NullLog.Info("SubsectorSelect", $"User selected subsector: {subsectorId}");
            OnSubsectorChosen?.Invoke(subsectorId);

            if (CampaignFlowManager.Instance != null)
            {
                CampaignFlowManager.Instance.SelectSubsector(subsectorId);
            }
        }

        public void Show()
        {
            if (_menuPanel != null) _menuPanel.SetActive(true);
            RefreshUI();
        }

        public void Hide()
        {
            if (_menuPanel != null && _menuPanel != gameObject)
            {
                _menuPanel.SetActive(false);
            }
        }
    }
}
