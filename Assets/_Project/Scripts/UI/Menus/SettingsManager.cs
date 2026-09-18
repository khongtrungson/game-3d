using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using NullProtocol.Core;
using NullProtocol.Controller;
using NullProtocol.Combat;
using NullProtocol.Gadgets;

namespace NullProtocol.UI
{
    /// <summary>
    /// Master manager and runtime bridge for Settings, Controls & Accessibility (FR-44, FR-45, FR-46).
    /// - FR-44: Full Keyboard & Mouse remapping, saving/loading overrides to/from JSON.
    /// - FR-45: Raw mouse input, sensitivity slider (0.1 to 10.0), mouse acceleration toggle, and Y-axis invert.
    /// - FR-46: Field of View (FOV) slider (80° to 110°, default 90°), independent volume sliders (Master, SFX, Radio Voice, UI),
    ///   and Wireframe Themes (Default Cyan/Orange, High-Contrast Yellow/Blue, Protanopia/Deuteranopia Mode).
    /// </summary>
    [DisallowMultipleComponent]
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Persistence & Input Configuration")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private AudioMixer _audioMixer;

        [Header("Audio Mixer Exposed Parameter Names")]
        [SerializeField] private string _masterVolumeParam = "MasterVolume";
        [SerializeField] private string _sfxVolumeParam = "SfxVolume";
        [SerializeField] private string _radioVoiceVolumeParam = "RadioVoiceVolume";
        [SerializeField] private string _uiVolumeParam = "UiVolume";

        [Header("Active Scene Entity References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private FirstPersonCameraLook _cameraLook;
        [SerializeField] private WeaponSwayAndADS _weaponSwayAndAds;
        [SerializeField] private WeaponReceiverDisplay _weaponReceiverDisplay;
        [SerializeField] private ForearmWatchDisplay _forearmWatchDisplay;

        private GameSettingsData _settings = new GameSettingsData();
        private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;

        public GameSettingsData CurrentSettings => _settings;
        public InputActionAsset InputActions => _inputActions;
        public WireframeTheme CurrentTheme => _settings.Theme;
        public WireframeThemePalette CurrentThemePalette => WireframeThemePalette.GetPalette(_settings.Theme);

        public event Action<GameSettingsData> OnSettingsChanged;
        public event Action<WireframeTheme> OnThemeChanged;
        public event Action<string> OnRebindComplete;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            FindSceneReferences();
            LoadSettingsFromProfile();
            ApplyAllSettings();
        }

        public void FindSceneReferences()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main ?? FindAnyObjectByType<Camera>();
            }

            if (_cameraLook == null)
            {
                _cameraLook = FindAnyObjectByType<FirstPersonCameraLook>();
            }

            if (_weaponSwayAndAds == null)
            {
                _weaponSwayAndAds = FindAnyObjectByType<WeaponSwayAndADS>();
            }

            if (_weaponReceiverDisplay == null)
            {
                _weaponReceiverDisplay = FindAnyObjectByType<WeaponReceiverDisplay>();
            }

            if (_forearmWatchDisplay == null)
            {
                _forearmWatchDisplay = FindAnyObjectByType<ForearmWatchDisplay>();
            }
        }

        #region Profile Persistence Integration

        public static Func<GameSettingsData> ProfileSettingsLoader { get; set; }
        public static Action<GameSettingsData> ProfileSettingsSaver { get; set; }

        public void LoadSettingsFromProfile()
        {
            if (ProfileSettingsLoader != null)
            {
                var profileSettings = ProfileSettingsLoader.Invoke();
                if (profileSettings != null)
                {
                    _settings = profileSettings;
                }
            }

            _settings.ValidateAndClamp();

            // Apply loaded input rebinding overrides (FR-44)
            if (!string.IsNullOrEmpty(_settings.InputOverridesJson) && _inputActions != null)
            {
                LoadBindingOverridesFromJson(_settings.InputOverridesJson);
            }
        }

        public void SaveSettingsToProfile()
        {
            _settings.ValidateAndClamp();

            if (_inputActions != null)
            {
                _settings.InputOverridesJson = SaveBindingOverridesToJson();
            }

            ProfileSettingsSaver?.Invoke(_settings);
        }

        #endregion

        #region Master Apply Method

        public void ApplyAllSettings()
        {
            ApplyMouseSettings();
            ApplyCameraAndFovSettings();
            ApplyAudioSettings();
            ApplyThemeSettings();

            OnSettingsChanged?.Invoke(_settings);
        }

        #endregion

        #region FR-45: Controls & Mouse Input Processing

        public void SetRawMouseInput(bool enabled)
        {
            _settings.RawMouseInput = enabled;
            ApplyMouseSettings();
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            _settings.MouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10.0f);
            ApplyMouseSettings();
        }

        public void SetMouseAcceleration(bool enabled)
        {
            _settings.MouseAcceleration = enabled;
            ApplyMouseSettings();
        }

        public void SetInvertY(bool enabled)
        {
            _settings.InvertY = enabled;
            ApplyMouseSettings();
        }

        private void ApplyMouseSettings()
        {
            if (_cameraLook != null)
            {
                _cameraLook.ApplySettings(_settings);
            }
        }

        #endregion

        #region FR-46: Camera FOV, Volume Sliders & Wireframe Themes

        public void SetFieldOfView(float fov)
        {
            _settings.FieldOfView = Mathf.Clamp(fov, 80.0f, 110.0f);
            ApplyCameraAndFovSettings();
        }

        private void ApplyCameraAndFovSettings()
        {
            if (_weaponSwayAndAds != null)
            {
                _weaponSwayAndAds.SetBaseFov(_settings.FieldOfView);
            }
            else if (_mainCamera != null)
            {
                _mainCamera.fieldOfView = _settings.FieldOfView;
            }
        }

        public void SetMasterVolume(float volume)
        {
            _settings.MasterVolume = Mathf.Clamp01(volume);
            ApplyAudioParameter(_masterVolumeParam, _settings.MasterVolume);
        }

        public void SetSfxVolume(float volume)
        {
            _settings.SfxVolume = Mathf.Clamp01(volume);
            ApplyAudioParameter(_sfxVolumeParam, _settings.SfxVolume);
        }

        public void SetRadioVoiceVolume(float volume)
        {
            _settings.RadioVoiceVolume = Mathf.Clamp01(volume);
            ApplyAudioParameter(_radioVoiceVolumeParam, _settings.RadioVoiceVolume);
        }

        public void SetUiVolume(float volume)
        {
            _settings.UiVolume = Mathf.Clamp01(volume);
            ApplyAudioParameter(_uiVolumeParam, _settings.UiVolume);
        }

        private void ApplyAudioSettings()
        {
            ApplyAudioParameter(_masterVolumeParam, _settings.MasterVolume);
            ApplyAudioParameter(_sfxVolumeParam, _settings.SfxVolume);
            ApplyAudioParameter(_radioVoiceVolumeParam, _settings.RadioVoiceVolume);
            ApplyAudioParameter(_uiVolumeParam, _settings.UiVolume);
        }

        private void ApplyAudioParameter(string paramName, float normalizedVolume)
        {
            if (_audioMixer != null && !string.IsNullOrEmpty(paramName))
            {
                // Standard logarithmic conversion: 0.0 -> -80dB, 1.0 -> 0dB
                float db = (normalizedVolume > 0.0001f) ? Mathf.Log10(normalizedVolume) * 20f : -80f;
                _audioMixer.SetFloat(paramName, db);
            }
        }

        public void SetWireframeTheme(WireframeTheme theme)
        {
            _settings.Theme = theme;
            ApplyThemeSettings();
            OnThemeChanged?.Invoke(theme);
        }

        private void ApplyThemeSettings()
        {
            if (_weaponReceiverDisplay != null)
            {
                _weaponReceiverDisplay.ApplyWireframeTheme(_settings.Theme);
            }

            if (_forearmWatchDisplay != null)
            {
                _forearmWatchDisplay.ApplyWireframeTheme(_settings.Theme);
            }
        }

        #endregion

        #region FR-44: Full Keyboard & Mouse Remapping

        /// <summary>
        /// Serializes all binding overrides on the action asset into JSON (FR-44).
        /// </summary>
        public string SaveBindingOverridesToJson()
        {
            if (_inputActions == null) return string.Empty;
            return _inputActions.SaveBindingOverridesAsJson();
        }

        /// <summary>
        /// Restores binding overrides on the action asset from JSON (FR-44).
        /// </summary>
        public void LoadBindingOverridesFromJson(string json)
        {
            if (_inputActions == null || string.IsNullOrEmpty(json)) return;
            _inputActions.LoadBindingOverridesFromJson(json);
        }

        /// <summary>
        /// Resets all bindings to their project defaults (FR-44).
        /// </summary>
        public void ResetAllBindingsToDefault()
        {
            if (_inputActions == null) return;

            foreach (var map in _inputActions.actionMaps)
            {
                map.RemoveAllBindingOverrides();
            }

            _settings.InputOverridesJson = string.Empty;
            SaveSettingsToProfile();
        }

        /// <summary>
        /// Starts interactive remapping for a given action and binding index (FR-44).
        /// Supports keyboard keys and mouse buttons.
        /// </summary>
        public void StartRebinding(
            string actionName,
            int bindingIndex = 0,
            Action onComplete = null,
            Action onCancel = null)
        {
            if (_inputActions == null)
            {
                NullLog.Error("SettingsManager", "Cannot rebind: InputActionAsset is null.");
                return;
            }

            var action = _inputActions.FindAction(actionName);
            if (action == null)
            {
                NullLog.Error("SettingsManager", $"Action not found: {actionName}");
                return;
            }

            action.Disable();
            _rebindingOperation?.Cancel();

            _rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Pointer>/delta")
                .WithControlsExcluding("<Mouse>/position")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation =>
                {
                    action.Enable();
                    operation.Dispose();
                    _rebindingOperation = null;

                    SaveSettingsToProfile();
                    OnRebindComplete?.Invoke(actionName);
                    onComplete?.Invoke();
                })
                .OnCancel(operation =>
                {
                    action.Enable();
                    operation.Dispose();
                    _rebindingOperation = null;
                    action.Enable();
                    onCancel?.Invoke();
                });

            _rebindingOperation.Start();
        }

        /// <summary>
        /// Programmatically sets a binding override string (e.g. for unit testing).
        /// </summary>
        public void OverrideBinding(string actionName, string newPath, int bindingIndex = 0)
        {
            if (_inputActions == null) return;
            var action = _inputActions.FindAction(actionName);
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count) return;

            action.ApplyBindingOverride(bindingIndex, newPath);
            SaveSettingsToProfile();
        }

        /// <summary>
        /// Gets human-readable display string for an action's binding.
        /// </summary>
        public string GetBindingDisplayString(string actionName, int bindingIndex = 0)
        {
            if (_inputActions == null) return string.Empty;
            var action = _inputActions.FindAction(actionName);
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count) return string.Empty;

            return action.GetBindingDisplayString(bindingIndex);
        }

        #endregion

        private void OnDestroy()
        {
            _rebindingOperation?.Cancel();
            _rebindingOperation?.Dispose();
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
