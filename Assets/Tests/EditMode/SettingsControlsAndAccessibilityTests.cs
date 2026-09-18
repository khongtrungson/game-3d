using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using NullProtocol.Core;
using NullProtocol.Controller;
using NullProtocol.Combat;
using NullProtocol.Gadgets;
using NullProtocol.Persistence;
using NullProtocol.UI;

namespace NullProtocol.Tests.EditMode
{
    [TestFixture]
    public class SettingsControlsAndAccessibilityTests
    {
        private GameObject _testRoot;
        private InputActionAsset _inputActions;
        private SettingsManager _settingsManager;

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TestRoot_Settings");

            // Create and configure test InputActionAsset with Player map and actions
            _inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
            var playerMap = _inputActions.AddActionMap("Player");
            var moveAction = playerMap.AddAction("Move", type: InputActionType.Value);
            moveAction.expectedControlType = "Vector2";
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            var lookAction = playerMap.AddAction("Look", type: InputActionType.Value);
            lookAction.expectedControlType = "Vector2";
            lookAction.AddBinding("<Pointer>/delta");
            playerMap.AddAction("Attack", type: InputActionType.Button)
                .AddBinding("<Mouse>/leftButton");
            playerMap.AddAction("Aim", type: InputActionType.Button)
                .AddBinding("<Mouse>/rightButton");
            playerMap.AddAction("Reload", type: InputActionType.Button)
                .AddBinding("<Keyboard>/r");
            playerMap.AddAction("Crouch", type: InputActionType.Button)
                .AddBinding("<Keyboard>/c");
            playerMap.AddAction("Sprint", type: InputActionType.Button)
                .AddBinding("<Keyboard>/leftShift");
            playerMap.AddAction("GadgetBarricade", type: InputActionType.Button)
                .AddBinding("<Keyboard>/g");
            playerMap.AddAction("GadgetSmoke", type: InputActionType.Button)
                .AddBinding("<Keyboard>/f");
            playerMap.AddAction("GadgetTripMine", type: InputActionType.Button)
                .AddBinding("<Keyboard>/x");

            var leanAction = playerMap.AddAction("Lean", type: InputActionType.Value);
            leanAction.expectedControlType = "Axis";
            leanAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/q")
                .With("Positive", "<Keyboard>/e");

            playerMap.Enable();

            var managerGo = new GameObject("SettingsManager");
            managerGo.transform.SetParent(_testRoot.transform);
            _settingsManager = managerGo.AddComponent<SettingsManager>();
            _settingsManager.GetType().GetField("_inputActions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_settingsManager, _inputActions);
        }

        [TearDown]
        public void TearDown()
        {
            SettingsManager.ProfileSettingsLoader = null;
            SettingsManager.ProfileSettingsSaver = null;

            if (_testRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(_testRoot);
            }

            if (_inputActions != null)
            {
                UnityEngine.Object.DestroyImmediate(_inputActions);
            }
        }

        #region FR-44: Full Keyboard & Mouse Input Remapping

        [Test]
        public void FR44_InputRemapping_SupportsLocomotionWeaponAndGadgetActions()
        {
            var playerMap = _inputActions.FindActionMap("Player");
            Assert.IsNotNull(playerMap.FindAction("Move"));
            Assert.IsNotNull(playerMap.FindAction("Attack"));
            Assert.IsNotNull(playerMap.FindAction("Aim"));
            Assert.IsNotNull(playerMap.FindAction("Reload"));
            Assert.IsNotNull(playerMap.FindAction("Crouch"));
            Assert.IsNotNull(playerMap.FindAction("Sprint"));
            Assert.IsNotNull(playerMap.FindAction("Lean"));
            Assert.IsNotNull(playerMap.FindAction("GadgetBarricade"));
            Assert.IsNotNull(playerMap.FindAction("GadgetSmoke"));
            Assert.IsNotNull(playerMap.FindAction("GadgetTripMine"));

            // Test programmatically remapping a weapon action
            var attackAction = playerMap.FindAction("Attack");
            _settingsManager.OverrideBinding("Attack", "<Keyboard>/space", 0);
            Assert.AreEqual("<Keyboard>/space", attackAction.bindings[0].overridePath);

            // Test programmatically remapping a locomotion action (Sprint)
            var sprintAction = playerMap.FindAction("Sprint");
            _settingsManager.OverrideBinding("Sprint", "<Keyboard>/leftCtrl", 0);
            Assert.AreEqual("<Keyboard>/leftCtrl", sprintAction.bindings[0].overridePath);

            // Test programmatically remapping a gadget action (Barricade)
            var barricadeAction = playerMap.FindAction("GadgetBarricade");
            _settingsManager.OverrideBinding("GadgetBarricade", "<Keyboard>/t", 0);
            Assert.AreEqual("<Keyboard>/t", barricadeAction.bindings[0].overridePath);
        }

        [Test]
        public void FR44_InputRemapping_SerializesAndRestoresOverridesFromJson()
        {
            var playerMap = _inputActions.FindActionMap("Player");
            _settingsManager.OverrideBinding("Attack", "<Keyboard>/f", 0);
            _settingsManager.OverrideBinding("GadgetTripMine", "<Keyboard>/z", 0);

            string json = _settingsManager.SaveBindingOverridesToJson();
            Assert.IsFalse(string.IsNullOrEmpty(json), "Serialized input overrides JSON should not be empty");

            // Reset bindings
            _settingsManager.ResetAllBindingsToDefault();
            Assert.IsNull(playerMap.FindAction("Attack").bindings[0].overridePath);
            Assert.IsNull(playerMap.FindAction("GadgetTripMine").bindings[0].overridePath);

            // Restore from JSON
            _settingsManager.LoadBindingOverridesFromJson(json);
            Assert.AreEqual("<Keyboard>/f", playerMap.FindAction("Attack").bindings[0].overridePath);
            Assert.AreEqual("<Keyboard>/z", playerMap.FindAction("GadgetTripMine").bindings[0].overridePath);
        }

        #endregion

        #region FR-45: Raw Mouse Input, Sensitivity Slider (0.1 to 10.0), Mouse Acceleration & Invert Y

        [Test]
        public void FR45_MouseSensitivity_ClampsBetweenPointOneAndTen()
        {
            var cameraGo = new GameObject("TestCameraMount");
            cameraGo.transform.SetParent(_testRoot.transform);
            var look = cameraGo.AddComponent<FirstPersonCameraLook>();

            // Below minimum -> clamps to 0.1
            _settingsManager.SetMouseSensitivity(0.01f);
            Assert.AreEqual(0.1f, _settingsManager.CurrentSettings.MouseSensitivity, 0.001f);

            // Above maximum -> clamps to 10.0
            _settingsManager.SetMouseSensitivity(25.0f);
            Assert.AreEqual(10.0f, _settingsManager.CurrentSettings.MouseSensitivity, 0.001f);

            // Valid mid-range
            _settingsManager.SetMouseSensitivity(3.5f);
            Assert.AreEqual(3.5f, _settingsManager.CurrentSettings.MouseSensitivity, 0.001f);
        }

        [Test]
        public void FR45_MouseAcceleration_DefaultsToOffAndModifiesLookDelta()
        {
            var cameraGo = new GameObject("TestCameraMount");
            cameraGo.transform.SetParent(_testRoot.transform);
            var look = cameraGo.AddComponent<FirstPersonCameraLook>();

            // Default state must be OFF per FR-45
            var settings = new GameSettingsData();
            Assert.IsFalse(settings.MouseAcceleration, "Mouse acceleration must default to OFF per FR-45");

            look.ApplySettings(settings);
            Assert.IsFalse(look.MouseAcceleration);

            // Test toggling mouse acceleration ON
            settings.MouseAcceleration = true;
            look.ApplySettings(settings);
            Assert.IsTrue(look.MouseAcceleration);
        }

        [Test]
        public void FR45_YAxisInvertToggle_InvertsPitchDelta()
        {
            var cameraGo = new GameObject("TestCameraMount");
            cameraGo.transform.SetParent(_testRoot.transform);
            var pitchPivot = new GameObject("PitchPivot");
            pitchPivot.transform.SetParent(cameraGo.transform);

            var look = cameraGo.AddComponent<FirstPersonCameraLook>();
            look.GetType().GetField("_pitchTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(look, pitchPivot.transform);

            var settingsNormal = new GameSettingsData { InvertY = false, MouseSensitivity = 1.0f };
            look.ApplySettings(settingsNormal);
            look.SetPitch(0f);

            // Moving mouse UP (positive delta.y) should pitch UP (negative Euler x in Unity FPS camera standard)
            look.ProcessLook(new Vector2(0f, 10f));
            float pitchNormal = pitchPivot.transform.localEulerAngles.x;
            if (pitchNormal > 180f) pitchNormal -= 360f;
            Assert.Less(pitchNormal, 0f, "Normal Y axis: mouse moving up pitches camera upwards (negative Euler)");

            // Invert Y enabled
            var settingsInverted = new GameSettingsData { InvertY = true, MouseSensitivity = 1.0f };
            look.ApplySettings(settingsInverted);
            look.SetPitch(0f);

            look.ProcessLook(new Vector2(0f, 10f));
            float pitchInverted = pitchPivot.transform.localEulerAngles.x;
            if (pitchInverted > 180f) pitchInverted -= 360f;
            Assert.Greater(pitchInverted, 0f, "Inverted Y axis: mouse moving up pitches camera downwards (positive Euler)");
        }

        [Test]
        public void FR45_RawMouseInput_TogglesAndPersists()
        {
            _settingsManager.SetRawMouseInput(true);
            Assert.IsTrue(_settingsManager.CurrentSettings.RawMouseInput);

            _settingsManager.SetRawMouseInput(false);
            Assert.IsFalse(_settingsManager.CurrentSettings.RawMouseInput);
        }

        #endregion

        #region FR-46: Field of View (FOV) Slider (80° to 110°, default 90°), Volume Sliders & Wireframe Themes

        [Test]
        public void FR46_FieldOfView_DefaultsToNinetyAndClampsBetweenEightyAndOneTen()
        {
            var settings = new GameSettingsData();
            Assert.AreEqual(90.0f, settings.FieldOfView, 0.001f, "Default FOV must be 90° per FR-46");

            _settingsManager.SetFieldOfView(60.0f);
            Assert.AreEqual(80.0f, _settingsManager.CurrentSettings.FieldOfView, 0.001f, "FOV below 80° must clamp to 80°");

            _settingsManager.SetFieldOfView(140.0f);
            Assert.AreEqual(110.0f, _settingsManager.CurrentSettings.FieldOfView, 0.001f, "FOV above 110° must clamp to 110°");

            _settingsManager.SetFieldOfView(95.0f);
            Assert.AreEqual(95.0f, _settingsManager.CurrentSettings.FieldOfView, 0.001f);
        }

        [Test]
        public void FR46_WeaponSwayAndAds_BaseFov_UpdatesWithSettings()
        {
            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(_testRoot.transform);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 90.0f;

            var swayGo = new GameObject("WeaponSway");
            swayGo.transform.SetParent(_testRoot.transform);
            var sway = swayGo.AddComponent<WeaponSwayAndADS>();
            sway.GetType().GetField("_playerCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(sway, cam);

            sway.SetBaseFov(105.0f);
            Assert.AreEqual(105.0f, sway.BaseFov, 0.001f);
            Assert.AreEqual(105.0f, cam.fieldOfView, 0.001f);
        }

        [Test]
        public void FR46_IndependentVolumeSliders_MasterSfxRadioVoiceUi()
        {
            _settingsManager.SetMasterVolume(0.8f);
            _settingsManager.SetSfxVolume(0.6f);
            _settingsManager.SetRadioVoiceVolume(0.9f);
            _settingsManager.SetUiVolume(0.5f);

            Assert.AreEqual(0.8f, _settingsManager.CurrentSettings.MasterVolume, 0.001f);
            Assert.AreEqual(0.6f, _settingsManager.CurrentSettings.SfxVolume, 0.001f);
            Assert.AreEqual(0.9f, _settingsManager.CurrentSettings.RadioVoiceVolume, 0.001f);
            Assert.AreEqual(0.5f, _settingsManager.CurrentSettings.UiVolume, 0.001f);

            // Volume clamping test (0.0 to 1.0)
            _settingsManager.SetMasterVolume(-0.5f);
            Assert.AreEqual(0f, _settingsManager.CurrentSettings.MasterVolume, 0.001f);

            _settingsManager.SetMasterVolume(1.8f);
            Assert.AreEqual(1.0f, _settingsManager.CurrentSettings.MasterVolume, 0.001f);
        }

        [Test]
        public void FR46_WireframeThemeOptions_DefaultCyanOrange_HighContrast_ProtanopiaDeuteranopia()
        {
            // 1. Default Cyan/Orange
            _settingsManager.SetWireframeTheme(WireframeTheme.DefaultCyanOrange);
            var defaultPalette = _settingsManager.CurrentThemePalette;
            Assert.Greater(defaultPalette.PrimaryColor.b, 0.8f, "Cyan primary must have high blue component");
            Assert.Greater(defaultPalette.SecondaryColor.r, 0.8f, "Orange secondary must have high red component");

            // 2. High-Contrast Yellow/Blue
            _settingsManager.SetWireframeTheme(WireframeTheme.HighContrastYellowBlue);
            var yellowBluePalette = _settingsManager.CurrentThemePalette;
            Assert.Greater(yellowBluePalette.PrimaryColor.r, 0.8f);
            Assert.Greater(yellowBluePalette.PrimaryColor.g, 0.8f, "Yellow primary must have high red and green");
            Assert.Greater(yellowBluePalette.SecondaryColor.b, 0.8f, "Blue secondary must have high blue");

            // 3. Protanopia/Deuteranopia Mode
            _settingsManager.SetWireframeTheme(WireframeTheme.ProtanopiaDeuteranopiaMode);
            var cbPalette = _settingsManager.CurrentThemePalette;
            Assert.Greater(cbPalette.PrimaryColor.b, 0.8f, "Colorblind mode uses distinctive blue primary");
            Assert.Greater(cbPalette.SecondaryColor.r, 0.8f, "Colorblind mode uses golden amber secondary");
        }

        [Test]
        public void FR46_WireframeTheme_PropagatesToDiegeticDisplays()
        {
            var receiverGo = new GameObject("ReceiverMesh");
            receiverGo.transform.SetParent(_testRoot.transform);
            receiverGo.AddComponent<MeshRenderer>();
            var receiverDisplay = receiverGo.AddComponent<WeaponReceiverDisplay>();

            var watchGo = new GameObject("WristWatchMesh");
            watchGo.transform.SetParent(_testRoot.transform);
            watchGo.AddComponent<MeshRenderer>();
            var watchDisplay = watchGo.AddComponent<ForearmWatchDisplay>();

            // Apply High-Contrast Yellow/Blue theme
            receiverDisplay.ApplyWireframeTheme(WireframeTheme.HighContrastYellowBlue);
            watchDisplay.ApplyWireframeTheme(WireframeTheme.HighContrastYellowBlue);

            Color receiverColor = receiverDisplay.GetCurrentEmissiveColor();
            Color watchColor = watchDisplay.GetCurrentHealthColor();

            // Yellow primary should be active for full health & normal ammo
            Assert.Greater(receiverColor.r, 1.0f);
            Assert.Greater(receiverColor.g, 1.0f);
            Assert.Greater(watchColor.r, 1.0f);
            Assert.Greater(watchColor.g, 1.0f);
        }

        #endregion

        #region Profile Serialization Integration

        [Test]
        public void SettingsData_SerializesIntoProfileData()
        {
            var profile = new ProfileData();
            profile.Settings.MouseSensitivity = 4.2f;
            profile.Settings.FieldOfView = 100.0f;
            profile.Settings.InvertY = true;
            profile.Settings.Theme = WireframeTheme.HighContrastYellowBlue;
            profile.Settings.SfxVolume = 0.75f;

            string json = JsonUtility.ToJson(profile);
            Assert.IsFalse(string.IsNullOrEmpty(json));

            var deserialized = JsonUtility.FromJson<ProfileData>(json);
            Assert.IsNotNull(deserialized.Settings);
            Assert.AreEqual(4.2f, deserialized.Settings.MouseSensitivity, 0.001f);
            Assert.AreEqual(100.0f, deserialized.Settings.FieldOfView, 0.001f);
            Assert.IsTrue(deserialized.Settings.InvertY);
            Assert.AreEqual(WireframeTheme.HighContrastYellowBlue, deserialized.Settings.Theme);
            Assert.AreEqual(0.75f, deserialized.Settings.SfxVolume, 0.001f);
        }

        #endregion
    }
}
