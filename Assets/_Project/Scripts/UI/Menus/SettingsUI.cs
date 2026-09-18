using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NullProtocol.Core;

namespace NullProtocol.UI
{
    /// <summary>
    /// UI Controller for the Settings Menu (FR-44, FR-45, FR-46).
    /// Provides interactive controls for:
    /// - Mouse Sensitivity slider (0.1 to 10.0), Raw Mouse Input toggle, Mouse Acceleration toggle, Y-Axis Invert toggle.
    /// - FOV slider (80° to 110°, default 90°).
    /// - Independent volume sliders: Master, SFX, Radio Voice, UI (0.0 to 1.0).
    /// - Wireframe Theme options (Default Cyan/Orange, High-Contrast Yellow/Blue, Protanopia/Deuteranopia Mode).
    /// - Full Keyboard & Mouse rebinding interface with Reset to Defaults button.
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsUI : MonoBehaviour
    {
        [Header("Root Menu Panel")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private SettingsManager _settingsManager;

        [Header("FR-45: Controls & Mouse Widgets")]
        [SerializeField] private Slider _mouseSensitivitySlider;
        [SerializeField] private TextMeshProUGUI _mouseSensitivityValueText;
        [SerializeField] private Toggle _rawMouseInputToggle;
        [SerializeField] private Toggle _mouseAccelerationToggle;
        [SerializeField] private Toggle _invertYToggle;

        [Header("FR-46: Camera & FOV Widgets")]
        [SerializeField] private Slider _fovSlider;
        [SerializeField] private TextMeshProUGUI _fovValueText;

        [Header("FR-46: Volume Sliders")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _radioVoiceVolumeSlider;
        [SerializeField] private Slider _uiVolumeSlider;

        [Header("FR-46: Wireframe Theme Dropdown / Options")]
        [SerializeField] private TMP_Dropdown _themeDropdown;

        [Header("FR-44: Rebinding Buttons & Labels")]
        [SerializeField] private Button _rebindMoveForwardButton;
        [SerializeField] private TextMeshProUGUI _rebindMoveForwardText;
        [SerializeField] private Button _rebindFireButton;
        [SerializeField] private TextMeshProUGUI _rebindFireText;
        [SerializeField] private Button _rebindAimButton;
        [SerializeField] private TextMeshProUGUI _rebindAimText;
        [SerializeField] private Button _rebindCrouchButton;
        [SerializeField] private TextMeshProUGUI _rebindCrouchText;
        [SerializeField] private Button _rebindSprintButton;
        [SerializeField] private TextMeshProUGUI _rebindSprintText;
        [SerializeField] private Button _rebindLeanLeftButton;
        [SerializeField] private TextMeshProUGUI _rebindLeanLeftText;
        [SerializeField] private Button _rebindLeanRightButton;
        [SerializeField] private TextMeshProUGUI _rebindLeanRightText;
        [SerializeField] private Button _rebindBarricadeButton;
        [SerializeField] private TextMeshProUGUI _rebindBarricadeText;
        [SerializeField] private Button _rebindSmokeButton;
        [SerializeField] private TextMeshProUGUI _rebindSmokeText;
        [SerializeField] private Button _rebindTripMineButton;
        [SerializeField] private TextMeshProUGUI _rebindTripMineText;
        [SerializeField] private Button _resetBindingsButton;
        [SerializeField] private TextMeshProUGUI _rebindingPromptText;

        [Header("Action Buttons")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _closeButton;

        private void Awake()
        {
            if (_settingsPanel == null)
            {
                _settingsPanel = gameObject;
            }

            if (_settingsManager == null)
            {
                _settingsManager = SettingsManager.Instance ?? FindFirstObjectByType<SettingsManager>();
            }

            HookWidgets();
        }

        private void OnEnable()
        {
            RefreshDisplay();
        }

        private void HookWidgets()
        {
            // FR-45: Controls
            if (_mouseSensitivitySlider != null)
            {
                _mouseSensitivitySlider.minValue = 0.1f;
                _mouseSensitivitySlider.maxValue = 10.0f;
                _mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            }

            if (_rawMouseInputToggle != null)
            {
                _rawMouseInputToggle.onValueChanged.AddListener(OnRawMouseInputChanged);
            }

            if (_mouseAccelerationToggle != null)
            {
                _mouseAccelerationToggle.onValueChanged.AddListener(OnMouseAccelerationChanged);
            }

            if (_invertYToggle != null)
            {
                _invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
            }

            // FR-46: FOV & Camera
            if (_fovSlider != null)
            {
                _fovSlider.minValue = 80.0f;
                _fovSlider.maxValue = 110.0f;
                _fovSlider.onValueChanged.AddListener(OnFovChanged);
            }

            // FR-46: Audio Volume Sliders
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.minValue = 0f;
                _masterVolumeSlider.maxValue = 1f;
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.minValue = 0f;
                _sfxVolumeSlider.maxValue = 1f;
                _sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (_radioVoiceVolumeSlider != null)
            {
                _radioVoiceVolumeSlider.minValue = 0f;
                _radioVoiceVolumeSlider.maxValue = 1f;
                _radioVoiceVolumeSlider.onValueChanged.AddListener(OnRadioVoiceVolumeChanged);
            }

            if (_uiVolumeSlider != null)
            {
                _uiVolumeSlider.minValue = 0f;
                _uiVolumeSlider.maxValue = 1f;
                _uiVolumeSlider.onValueChanged.AddListener(OnUiVolumeChanged);
            }

            // FR-46: Themes
            if (_themeDropdown != null)
            {
                _themeDropdown.ClearOptions();
                _themeDropdown.AddOptions(new List<string>
                {
                    "Default Cyan/Orange",
                    "High-Contrast Yellow/Blue",
                    "Protanopia/Deuteranopia Mode"
                });
                _themeDropdown.onValueChanged.AddListener(OnThemeDropdownChanged);
            }

            // FR-44: Rebinding
            HookRebindButton(_rebindFireButton, "Attack", 1);
            HookRebindButton(_rebindAimButton, "Aim", 0);
            HookRebindButton(_rebindCrouchButton, "Crouch", 1);
            HookRebindButton(_rebindSprintButton, "Sprint", 0);
            HookRebindButton(_rebindBarricadeButton, "GadgetBarricade", 0);
            HookRebindButton(_rebindSmokeButton, "GadgetSmoke", 0);
            HookRebindButton(_rebindTripMineButton, "GadgetTripMine", 0);

            if (_resetBindingsButton != null)
            {
                _resetBindingsButton.onClick.AddListener(OnResetBindingsClicked);
            }

            if (_saveButton != null)
            {
                _saveButton.onClick.AddListener(OnSaveClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }
        }

        private void HookRebindButton(Button button, string actionName, int bindingIndex)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => StartInteractiveRebind(actionName, bindingIndex));
            }
        }

        public void RefreshDisplay()
        {
            if (_settingsManager == null) return;
            var settings = _settingsManager.CurrentSettings;

            if (_mouseSensitivitySlider != null)
            {
                _mouseSensitivitySlider.value = settings.MouseSensitivity;
            }
            if (_mouseSensitivityValueText != null)
            {
                _mouseSensitivityValueText.text = settings.MouseSensitivity.ToString("F1");
            }

            if (_rawMouseInputToggle != null) _rawMouseInputToggle.isOn = settings.RawMouseInput;
            if (_mouseAccelerationToggle != null) _mouseAccelerationToggle.isOn = settings.MouseAcceleration;
            if (_invertYToggle != null) _invertYToggle.isOn = settings.InvertY;

            if (_fovSlider != null)
            {
                _fovSlider.value = settings.FieldOfView;
            }
            if (_fovValueText != null)
            {
                _fovValueText.text = $"{Mathf.RoundToInt(settings.FieldOfView)}°";
            }

            if (_masterVolumeSlider != null) _masterVolumeSlider.value = settings.MasterVolume;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = settings.SfxVolume;
            if (_radioVoiceVolumeSlider != null) _radioVoiceVolumeSlider.value = settings.RadioVoiceVolume;
            if (_uiVolumeSlider != null) _uiVolumeSlider.value = settings.UiVolume;

            if (_themeDropdown != null)
            {
                _themeDropdown.value = (int)settings.Theme;
            }

            RefreshRebindLabels();
        }

        public void RefreshRebindLabels()
        {
            if (_settingsManager == null) return;

            UpdateBindingLabel(_rebindFireText, "Attack", 1);
            UpdateBindingLabel(_rebindAimText, "Aim", 0);
            UpdateBindingLabel(_rebindCrouchText, "Crouch", 1);
            UpdateBindingLabel(_rebindSprintText, "Sprint", 0);
            UpdateBindingLabel(_rebindBarricadeText, "GadgetBarricade", 0);
            UpdateBindingLabel(_rebindSmokeText, "GadgetSmoke", 0);
            UpdateBindingLabel(_rebindTripMineText, "GadgetTripMine", 0);
        }

        private void UpdateBindingLabel(TextMeshProUGUI label, string actionName, int bindingIndex)
        {
            if (label != null && _settingsManager != null)
            {
                label.text = _settingsManager.GetBindingDisplayString(actionName, bindingIndex);
            }
        }

        #region Widget Callbacks

        private void OnMouseSensitivityChanged(float val)
        {
            _settingsManager?.SetMouseSensitivity(val);
            if (_mouseSensitivityValueText != null)
            {
                _mouseSensitivityValueText.text = val.ToString("F1");
            }
        }

        private void OnRawMouseInputChanged(bool val) => _settingsManager?.SetRawMouseInput(val);
        private void OnMouseAccelerationChanged(bool val) => _settingsManager?.SetMouseAcceleration(val);
        private void OnInvertYChanged(bool val) => _settingsManager?.SetInvertY(val);

        private void OnFovChanged(float val)
        {
            _settingsManager?.SetFieldOfView(val);
            if (_fovValueText != null)
            {
                _fovValueText.text = $"{Mathf.RoundToInt(val)}°";
            }
        }

        private void OnMasterVolumeChanged(float val) => _settingsManager?.SetMasterVolume(val);
        private void OnSfxVolumeChanged(float val) => _settingsManager?.SetSfxVolume(val);
        private void OnRadioVoiceVolumeChanged(float val) => _settingsManager?.SetRadioVoiceVolume(val);
        private void OnUiVolumeChanged(float val) => _settingsManager?.SetUiVolume(val);

        private void OnThemeDropdownChanged(int index)
        {
            _settingsManager?.SetWireframeTheme((WireframeTheme)index);
        }

        private void StartInteractiveRebind(string actionName, int bindingIndex)
        {
            if (_rebindingPromptText != null)
            {
                _rebindingPromptText.gameObject.SetActive(true);
                _rebindingPromptText.text = $"Press any key/button to bind [{actionName}] (ESC to cancel)...";
            }

            _settingsManager?.StartRebinding(actionName, bindingIndex,
                onComplete: () =>
                {
                    if (_rebindingPromptText != null) _rebindingPromptText.gameObject.SetActive(false);
                    RefreshRebindLabels();
                },
                onCancel: () =>
                {
                    if (_rebindingPromptText != null) _rebindingPromptText.gameObject.SetActive(false);
                    RefreshRebindLabels();
                });
        }

        private void OnResetBindingsClicked()
        {
            _settingsManager?.ResetAllBindingsToDefault();
            RefreshRebindLabels();
        }

        private void OnSaveClicked()
        {
            _settingsManager?.SaveSettingsToProfile();
            NullLog.Info("SettingsUI", "Settings saved successfully to profile.");
        }

        #endregion

        public void Show()
        {
            if (_settingsPanel != null) _settingsPanel.SetActive(true);
            RefreshDisplay();
        }

        public void Hide()
        {
            if (_settingsPanel != null && _settingsPanel != gameObject)
            {
                _settingsPanel.SetActive(false);
            }
        }
    }
}
