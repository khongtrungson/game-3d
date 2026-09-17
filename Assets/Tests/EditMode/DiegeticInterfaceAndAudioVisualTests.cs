using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.Combat;
using NullProtocol.Controller;
using NullProtocol.Gadgets;
using NullProtocol.UI;

namespace NullProtocol.Tests.EditMode
{
    [TestFixture]
    public class DiegeticInterfaceAndAudioVisualTests
    {
        private GameObject _hostGo;
        private PlayerStateEventChannelSO _playerStateEvents;
        private GadgetEventChannelSO _gadgetEvents;
        private KillEventChannelSO _killChannel;

        [SetUp]
        public void SetUp()
        {
            _hostGo = new GameObject("TestDiegeticHost");
            _playerStateEvents = ScriptableObject.CreateInstance<PlayerStateEventChannelSO>();
            _gadgetEvents = ScriptableObject.CreateInstance<GadgetEventChannelSO>();
            _killChannel = ScriptableObject.CreateInstance<KillEventChannelSO>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_hostGo != null)
            {
                Object.DestroyImmediate(_hostGo);
            }

            if (_playerStateEvents != null) Object.DestroyImmediate(_playerStateEvents);
            if (_gadgetEvents != null) Object.DestroyImmediate(_gadgetEvents);
            if (_killChannel != null) Object.DestroyImmediate(_killChannel);
        }

        #region FR-34: Weapon Receiver Emissive Numerals & Fire Mode

        [Test]
        public void FR34_WeaponReceiverDisplay_UpdatesAmmoAndFireModeDirectly()
        {
            var receiverGo = new GameObject("ReceiverMesh");
            receiverGo.transform.SetParent(_hostGo.transform);
            var meshRenderer = receiverGo.AddComponent<MeshRenderer>();
            var display = receiverGo.AddComponent<WeaponReceiverDisplay>();

            // Initial defaults
            Assert.AreEqual(12, display.CurrentAmmo);
            Assert.AreEqual(0, display.FireMode);

            // Update ammo
            display.SetAmmo(7, 20);
            Assert.AreEqual(7, display.CurrentAmmo);
            Assert.AreEqual(20, display.MaxAmmo);

            // Update fire mode (1 = Burst)
            display.SetFireMode(1);
            Assert.AreEqual(1, display.FireMode);

            // Color should be normal cyan for 7/20 ammo
            Color normalColor = display.GetCurrentEmissiveColor();
            Assert.Greater(normalColor.g, 0.5f);

            // Set critical ammo (<= 1)
            display.SetAmmo(1, 20);
            Color criticalColor = display.GetCurrentEmissiveColor();
            Assert.Greater(criticalColor.r, criticalColor.b, "Critical ammo should exhibit strong red hue");
        }

        [Test]
        public void FR34_WeaponDiegeticBinder_SyncsWithBaseWeapon()
        {
            var weaponGo = new GameObject("TestSynapseAR");
            weaponGo.transform.SetParent(_hostGo.transform);
            var synapse = weaponGo.AddComponent<SynapseAR>();
            var receiverGo = new GameObject("ReceiverMesh");
            receiverGo.transform.SetParent(weaponGo.transform);
            receiverGo.AddComponent<MeshRenderer>();
            var display = receiverGo.AddComponent<WeaponReceiverDisplay>();

            var binder = weaponGo.AddComponent<WeaponDiegeticBinder>();
            binder.SyncInitialState();

            // Toggle fire mode on SynapseAR
            synapse.SetFireMode(FireMode.Burst);
            binder.SyncInitialState();

            Assert.AreEqual(1, display.FireMode, "Synapse-AR Burst fire mode must propagate to display as 1");

            synapse.SetFireMode(FireMode.SemiAuto);
            binder.SyncInitialState();
            Assert.AreEqual(0, display.FireMode, "Synapse-AR SemiAuto mode must propagate to display as 0");
        }

        #endregion

        #region FR-35: In-World OLED Wrist Forearm Display

        [Test]
        public void FR35_ForearmWatchDisplay_ReflectsPlayerHPAndGadgetCounts()
        {
            var watchGo = new GameObject("WristWatchOLED");
            watchGo.transform.SetParent(_hostGo.transform);
            watchGo.AddComponent<MeshRenderer>();
            var watchDisplay = watchGo.AddComponent<ForearmWatchDisplay>();

            // Update health
            watchDisplay.SetHealth(80, 100);
            Assert.AreEqual(80, watchDisplay.CurrentHealth);
            Assert.AreEqual(100, watchDisplay.MaxHealth);

            // Update gadget charges (2 Barricades, 2 Smoke, 1 Mine per FR-16, 18, 20)
            watchDisplay.SetGadgetCharges(2, 2, 1);
            Assert.AreEqual(2, watchDisplay.BarricadeCharges);
            Assert.AreEqual(2, watchDisplay.SmokeCharges);
            Assert.AreEqual(1, watchDisplay.TripMineCharges);

            // Spend a barricade charge
            watchDisplay.SetGadgetCharge(GadgetType.HardLightBarricade, 1);
            Assert.AreEqual(1, watchDisplay.BarricadeCharges);

            // Health color check: 80 HP is healthy cyan
            Color healthyColor = watchDisplay.GetCurrentHealthColor();
            Assert.Greater(healthyColor.b, 0.5f);

            // Health drops below 25% -> critical red
            watchDisplay.SetHealth(20, 100);
            Color critColor = watchDisplay.GetCurrentHealthColor();
            Assert.Greater(critColor.r, 0.5f);
        }

        #endregion

        #region FR-36: Diegetic Viewport Enforcer (No Traditional 2D Overlays)

        [Test]
        public void FR36_DiegeticViewportEnforcer_SuppressesFloatingHUDs()
        {
            var enforcerGo = new GameObject("ViewportEnforcer");
            enforcerGo.transform.SetParent(_hostGo.transform);
            var enforcer = enforcerGo.AddComponent<DiegeticViewportEnforcer>();

            // Create a prohibited floating HUD canvas
            var prohibitedCanvasGo = new GameObject("MiniMap_ScreenOverlayHUD");
            var canvas = prohibitedCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Create a compliant world-space diegetic canvas
            var diegeticCanvasGo = new GameObject("Forearm_OLED_WorldCanvas");
            var diegeticCanvas = diegeticCanvasGo.AddComponent<Canvas>();
            diegeticCanvas.renderMode = RenderMode.WorldSpace;

            Assert.IsFalse(DiegeticViewportEnforcer.IsCanvasCompliant(canvas), "Screen-space minimap canvas should be non-compliant with FR-36");
            Assert.IsTrue(DiegeticViewportEnforcer.IsCanvasCompliant(diegeticCanvas), "World-space canvas is diegetic and compliant with FR-36");

            // Execute suppression
            enforcer.EnforceDiegeticViewport();
            Assert.IsFalse(prohibitedCanvasGo.activeSelf, "Prohibited floating HUD canvas must be suppressed/disabled");
            Assert.IsTrue(diegeticCanvasGo.activeSelf, "World-space diegetic canvas must remain active");

            Object.DestroyImmediate(prohibitedCanvasGo);
            Object.DestroyImmediate(diegeticCanvasGo);
        }

        #endregion

        #region FR-37: Flat-Shaded Geometry & Localized Chromatic Glitch

        [Test]
        public void FR37_LocalizedChromaticGlitchController_SpikesIntensityOnTrigger()
        {
            var glitchTarget = new GameObject("GlitchMesh");
            glitchTarget.transform.SetParent(_hostGo.transform);
            glitchTarget.AddComponent<MeshRenderer>();

            var glitchController = glitchTarget.AddComponent<LocalizedChromaticGlitchController>();
            glitchController.ApplyIntensity(0.75f);

            Assert.AreEqual(0.75f, glitchController.CurrentIntensity, 0.001f);

            glitchController.ApplyIntensity(0f);
            Assert.AreEqual(0f, glitchController.CurrentIntensity, 0.001f);
        }

        #endregion

        #region FR-38: Eliminated Sentinel 0.15s Freeze & 30-50 Voxel Shatter

        [Test]
        public void FR38_SentinelVoxelShatterDeath_TimingAndVoxelConfiguration()
        {
            var sentinelGo = new GameObject("Sentinel_VoxelTarget");
            sentinelGo.transform.SetParent(_hostGo.transform);
            sentinelGo.AddComponent<MeshRenderer>();
            var health = sentinelGo.AddComponent<SentinelHealth>();
            var voxelDeath = sentinelGo.AddComponent<SentinelVoxelShatterDeath>();

            // FR-38 strictly specifies: freeze for 0.15s, shatter into 30-50 voxels, dissolve over 2.0s
            Assert.AreEqual(0.15f, voxelDeath.FreezeDuration, 0.001f, "Pre-shatter freeze delay must be exactly 0.15s per FR-38");
            Assert.AreEqual(2.0f, voxelDeath.DissolveDuration, 0.001f, "Voxel dissolution must take 2.0s per FR-38");
            Assert.GreaterOrEqual(voxelDeath.VoxelCount, 30, "Voxel count must be >= 30");
            Assert.LessOrEqual(voxelDeath.VoxelCount, 50, "Voxel count must be <= 50");

            // Trigger shatter
            voxelDeath.TriggerShatterDeath(sentinelGo.transform.position, Vector3.up);
            Assert.IsTrue(voxelDeath.IsShattered);

            // Test ResetState restores entity
            voxelDeath.ResetState();
            Assert.IsFalse(voxelDeath.IsShattered);
            Assert.AreEqual(0, voxelDeath.SpawnedVoxels.Count);
        }

        #endregion

        #region FR-39: 3D HRTF Binaural Spatialization Audio

        [Test]
        public void FR39_BinauralAudioSpatializer_Enforces3DSpatialBlendAndHRTF()
        {
            var audioGo = new GameObject("AudioEmitter");
            audioGo.transform.SetParent(_hostGo.transform);
            var source = audioGo.AddComponent<AudioSource>();

            BinauralAudioSpatializer.ConfigureBinauralSource(source, 1.5f, 30.0f);

            Assert.AreEqual(1.0f, source.spatialBlend, "AudioSource spatialBlend must be 1.0 (100% 3D spatialized)");
            Assert.IsTrue(source.spatialize, "AudioSource spatialize must be enabled for HRTF engine");
            Assert.AreEqual(0f, source.spread, "Spread must be 0 for pinpoint 3D directional localization");
            Assert.AreEqual(1.5f, source.minDistance);
            Assert.AreEqual(30.0f, source.maxDistance);
        }

        [Test]
        public void FR39_LocomotionAcousticEmitter_Configures3DSpatialSource()
        {
            var playerGo = new GameObject("PlayerLocomotion");
            playerGo.transform.SetParent(_hostGo.transform);
            var audioSource = playerGo.AddComponent<AudioSource>();
            var emitter = playerGo.AddComponent<LocomotionAcousticEmitter>();

            // AudioSource spatialBlend should be configured for 3D HRTF
            Assert.AreEqual(1.0f, audioSource.spatialBlend, "Footstep emitter must be 100% 3D spatialized");
            Assert.IsTrue(audioSource.spatialize, "Footstep audio must have HRTF spatialization enabled");
        }

        #endregion
    }
}
