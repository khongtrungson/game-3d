using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NullProtocol.Core;
using NullProtocol.World;

namespace NullProtocol.UI
{
    /// <summary>
    /// Tactical Debrief screen presented upon subsector extraction (FR-33).
    /// Displays elapsed time, accuracy percentage, damage taken, and tactical grade (S, A, B, C).
    /// </summary>
    [DisallowMultipleComponent]
    public class TacticalDebriefUI : MonoBehaviour
    {
        [Header("Root Panel")]
        [SerializeField] private GameObject _debriefPanel;

        [Header("TMP Text Elements (FR-33)")]
        [SerializeField] private TextMeshProUGUI _subsectorTitleText;
        [SerializeField] private TextMeshProUGUI _elapsedTimeText;
        [SerializeField] private TextMeshProUGUI _accuracyText;
        [SerializeField] private TextMeshProUGUI _damageTakenText;
        [SerializeField] private TextMeshProUGUI _tacticalGradeText;

        [Header("Legacy UI Text Fallback (Optional)")]
        [SerializeField] private Text _legacyElapsedTimeText;
        [SerializeField] private Text _legacyAccuracyText;
        [SerializeField] private Text _legacyDamageTakenText;
        [SerializeField] private Text _legacyTacticalGradeText;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _proceedButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _subsectorSelectButton;

        [Header("Grade Color Scheme")]
        [SerializeField] private Color _gradeSColor = new Color(0.1f, 1.0f, 0.9f); // Emissive Cyan
        [SerializeField] private Color _gradeAColor = new Color(0.2f, 0.9f, 0.3f); // Tactical Green
        [SerializeField] private Color _gradeBColor = new Color(1.0f, 0.75f, 0.1f); // Warning Amber
        [SerializeField] private Color _gradeCColor = new Color(1.0f, 0.2f, 0.2f); // Glitch Red

        [Header("Dependencies")]
        [SerializeField] private LevelStatisticsTracker _statsTracker;

        private TacticalDebriefData _lastDebriefData;
        private bool _isDisplaying;

        public bool IsDisplaying => _isDisplaying;
        public TacticalDebriefData LastDebriefData => _lastDebriefData;

        public event Action OnProceedRequested;
        public event Action OnRetryRequested;
        public event Action OnSubsectorSelectRequested;

        private void Awake()
        {
            if (_debriefPanel == null)
            {
                _debriefPanel = gameObject;
            }

            if (_statsTracker == null)
            {
                _statsTracker = FindAnyObjectByType<LevelStatisticsTracker>();
            }

            HookButtons();
            HideDebrief();
        }

        private void OnEnable()
        {
            if (_statsTracker != null)
            {
                _statsTracker.OnDebriefReady += ShowDebrief;
            }
        }

        private void OnDisable()
        {
            if (_statsTracker != null)
            {
                _statsTracker.OnDebriefReady -= ShowDebrief;
            }
        }

        private void HookButtons()
        {
            if (_proceedButton != null) _proceedButton.onClick.AddListener(HandleProceedClicked);
            if (_retryButton != null) _retryButton.onClick.AddListener(HandleRetryClicked);
            if (_subsectorSelectButton != null) _subsectorSelectButton.onClick.AddListener(HandleSubsectorSelectClicked);
        }

        /// <summary>
        /// Populates and displays the Tactical Debrief screen (FR-33).
        /// </summary>
        public void ShowDebrief(TacticalDebriefData data)
        {
            _lastDebriefData = data;
            _isDisplaying = true;

            if (_debriefPanel != null)
            {
                _debriefPanel.SetActive(true);
            }

            UpdateDisplayElements(data);
            NullLog.Info("DebriefUI", $"Displaying Tactical Debrief: Grade {data.Grade}, Time: {data.FormattedTime}, Acc: {data.FormattedAccuracy}, Dmg: {data.FormattedDamage}");
        }

        private void UpdateDisplayElements(TacticalDebriefData data)
        {
            // TMP Text Updates
            if (_subsectorTitleText != null) _subsectorTitleText.text = $"{data.SubsectorTitle} // TACTICAL DEBRIEF";
            if (_elapsedTimeText != null) _elapsedTimeText.text = data.FormattedTime;
            if (_accuracyText != null) _accuracyText.text = data.FormattedAccuracy;
            if (_damageTakenText != null) _damageTakenText.text = data.FormattedDamage;

            if (_tacticalGradeText != null)
            {
                _tacticalGradeText.text = data.GradeString;
                _tacticalGradeText.color = GetGradeColor(data.Grade);
            }

            // Legacy Text Fallback
            if (_legacyElapsedTimeText != null) _legacyElapsedTimeText.text = data.FormattedTime;
            if (_legacyAccuracyText != null) _legacyAccuracyText.text = data.FormattedAccuracy;
            if (_legacyDamageTakenText != null) _legacyDamageTakenText.text = data.FormattedDamage;
            if (_legacyTacticalGradeText != null)
            {
                _legacyTacticalGradeText.text = data.GradeString;
                _legacyTacticalGradeText.color = GetGradeColor(data.Grade);
            }
        }

        public Color GetGradeColor(TacticalGrade grade)
        {
            return grade switch
            {
                TacticalGrade.S => _gradeSColor,
                TacticalGrade.A => _gradeAColor,
                TacticalGrade.B => _gradeBColor,
                TacticalGrade.C => _gradeCColor,
                _ => Color.white
            };
        }

        public void HideDebrief()
        {
            _isDisplaying = false;
            if (_debriefPanel != null && _debriefPanel != gameObject)
            {
                _debriefPanel.SetActive(false);
            }
        }

        public void HandleProceedClicked()
        {
            NullLog.Info("DebriefUI", "Proceed button clicked.");
            OnProceedRequested?.Invoke();

            if (CampaignFlowManager.Instance != null)
            {
                CampaignFlowManager.Instance.AdvanceToNextSubsector();
            }
        }

        public void HandleRetryClicked()
        {
            NullLog.Info("DebriefUI", "Retry button clicked.");
            OnRetryRequested?.Invoke();

            if (CampaignFlowManager.Instance != null)
            {
                CampaignFlowManager.Instance.SelectSubsector(_lastDebriefData.Subsector);
            }
        }

        public void HandleSubsectorSelectClicked()
        {
            NullLog.Info("DebriefUI", "Subsector Select button clicked.");
            OnSubsectorSelectRequested?.Invoke();
        }
    }
}
