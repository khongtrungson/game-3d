using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.Combat;

namespace NullProtocol.Tests.EditMode
{
    [TestFixture]
    public class BallisticsAndWeaponsTests
    {
        private GameObject _weaponHostGo;
        private DamageEventChannelSO _damageChannel;
        private KillEventChannelSO _killChannel;
        private AcousticStimulusEventChannelSO _acousticChannel;
        private GadgetEventChannelSO _gadgetChannel;

        [SetUp]
        public void SetUp()
        {
            _weaponHostGo = new GameObject("TestWeaponHost");
            _damageChannel = ScriptableObject.CreateInstance<DamageEventChannelSO>();
            _killChannel = ScriptableObject.CreateInstance<KillEventChannelSO>();
            _acousticChannel = ScriptableObject.CreateInstance<AcousticStimulusEventChannelSO>();
            _gadgetChannel = ScriptableObject.CreateInstance<GadgetEventChannelSO>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_weaponHostGo != null)
            {
                Object.DestroyImmediate(_weaponHostGo);
            }
            if (_damageChannel != null) Object.DestroyImmediate(_damageChannel);
            if (_killChannel != null) Object.DestroyImmediate(_killChannel);
            if (_acousticChannel != null) Object.DestroyImmediate(_acousticChannel);
            if (_gadgetChannel != null) Object.DestroyImmediate(_gadgetChannel);
        }

        private WeaponDataSO CreateTestWeaponData(WeaponId id)
        {
            var data = ScriptableObject.CreateInstance<WeaponDataSO>();
            data.WeaponId = id;
            switch (id)
            {
                case WeaponId.Vector9:
                    data.WeaponName = "Vector-9";
                    data.MagazineCapacity = 12;
                    data.InitialSpareAmmo = 36;
                    data.RoundsPerMinute = 400f;
                    data.BodyDamage = 30;
                    data.HeadDamage = 75;
                    data.IsSilenced = true;
                    data.ReloadTime = 1.2f;
                    data.RecoilPitchAngle = 0.8f;
                    data.PenetratesBarricades = false;
                    data.PenetratesOneWall = false;
                    break;
                case WeaponId.SynapseAR:
                    data.WeaponName = "Synapse-AR";
                    data.MagazineCapacity = 20;
                    data.InitialSpareAmmo = 60;
                    data.RoundsPerMinute = 550f;
                    data.BurstRoundsPerMinute = 550f;
                    data.BodyDamage = 45;
                    data.HeadDamage = 112;
                    data.IsSilenced = false;
                    data.ReloadTime = 1.9f;
                    data.RecoilPitchAngle = 2.2f;
                    data.PenetratesBarricades = true;
                    data.PenetratesOneWall = false;
                    break;
                case WeaponId.PhaseRail:
                    data.WeaponName = "Phase-Rail";
                    data.MagazineCapacity = 1;
                    data.InitialSpareAmmo = 3;
                    data.ChargeTime = 0.6f;
                    data.BodyDamage = 150;
                    data.HeadDamage = 999;
                    data.DisintegratesOnHeadshot = true;
                    data.IsSilenced = false;
                    data.ReloadTime = 1.8f;
                    data.RecoilPitchAngle = 6.0f;
                    data.PenetratesBarricades = false;
                    data.PenetratesOneWall = true;
                    break;
            }
            return data;
        }

        [Test]
        public void FR10_Vector9_Specifications_MatchDesignDocument()
        {
            var data = CreateTestWeaponData(WeaponId.Vector9);
            Assert.AreEqual(12, data.MagazineCapacity);
            Assert.AreEqual(400f, data.RoundsPerMinute);
            Assert.AreEqual(30, data.BodyDamage);
            Assert.AreEqual(75, data.HeadDamage);
            Assert.IsTrue(data.IsSilenced);
            Assert.AreEqual(1.2f, data.ReloadTime);
            Assert.IsFalse(data.PenetratesBarricades);
            Assert.IsFalse(data.PenetratesOneWall);
        }

        [Test]
        public void FR11_SynapseAR_Specifications_MatchDesignDocument()
        {
            var data = CreateTestWeaponData(WeaponId.SynapseAR);
            Assert.AreEqual(20, data.MagazineCapacity);
            Assert.AreEqual(550f, data.RoundsPerMinute);
            Assert.AreEqual(45, data.BodyDamage);
            Assert.AreEqual(112, data.HeadDamage);
            Assert.AreEqual(2.2f, data.RecoilPitchAngle, 0.001f);
            Assert.AreEqual(1.9f, data.ReloadTime);
            Assert.IsTrue(data.PenetratesBarricades);
            Assert.IsFalse(data.PenetratesOneWall);
        }

        [Test]
        public void FR12_PhaseRail_Specifications_MatchDesignDocument()
        {
            var data = CreateTestWeaponData(WeaponId.PhaseRail);
            Assert.AreEqual(1, data.MagazineCapacity);
            Assert.AreEqual(3, data.InitialSpareAmmo);
            Assert.AreEqual(0.6f, data.ChargeTime);
            Assert.AreEqual(150, data.BodyDamage);
            Assert.IsTrue(data.DisintegratesOnHeadshot);
            Assert.AreEqual(6.0f, data.RecoilPitchAngle, 0.001f);
            Assert.AreEqual(1.8f, data.ReloadTime);
            Assert.IsTrue(data.PenetratesOneWall);
        }

        [Test]
        public void FR13_Lethality_PlayerTakes35HpPerBullet_StandardSentinelDiesToOneTapHeadshot()
        {
            // Verify Player dies in 3 hits from 35 HP bullets
            var playerGo = new GameObject("PlayerTester");
            var playerHealth = playerGo.AddComponent<NullProtocol.Controller.PlayerHealth>();

            Assert.AreEqual(100, playerHealth.CurrentHealth);
            playerHealth.TakeDamage(35, Vector3.zero, Vector3.up, false);
            Assert.AreEqual(65, playerHealth.CurrentHealth);
            playerHealth.TakeDamage(35, Vector3.zero, Vector3.up, false);
            Assert.AreEqual(30, playerHealth.CurrentHealth);
            playerHealth.TakeDamage(35, Vector3.zero, Vector3.up, false);
            Assert.AreEqual(0, playerHealth.CurrentHealth);
            Assert.IsTrue(playerHealth.IsDead);

            // Verify Standard Sentinel dies in 1 headshot tap
            var sentinelGo = new GameObject("SentinelTester");
            var sentinelHealth = sentinelGo.AddComponent<SentinelHealth>();
            Assert.AreEqual(80, sentinelHealth.CurrentHealth);

            sentinelHealth.TakeDamage(75, Vector3.zero, Vector3.up, true); // Headshot
            Assert.AreEqual(0, sentinelHealth.CurrentHealth);
            Assert.IsTrue(sentinelHealth.IsDead);

            Object.DestroyImmediate(playerGo);
            Object.DestroyImmediate(sentinelGo);
        }

        [Test]
        public void FR14_ADS_NarrowFovBy15Percent_AndDampenSwayBy80Percent()
        {
            var camGo = new GameObject("TestCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 80.0f;

            var swayGo = new GameObject("TestSway");
            var sway = swayGo.AddComponent<WeaponSwayAndADS>();

            // Use reflection or private field setup via SerializedObject emulation
            var camField = typeof(WeaponSwayAndADS).GetField("_playerCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            camField?.SetValue(sway, cam);

            // Verify FOV multiplier is 0.85 (15% reduction)
            Assert.AreEqual(0.85f, sway.AdsFovMultiplier, 0.001f);
            // Verify Sway multiplier is 0.2 (80% damping)
            Assert.AreEqual(0.2f, sway.AdsSwayMultiplier, 0.001f);

            // Enable ADS
            sway.SetAimDownSights(true);
            Assert.IsTrue(sway.IsAiming);

            // Tick ADS transitions
            for (int i = 0; i < 50; i++)
            {
                sway.TickADSAndSway(0.05f, Vector2.zero);
            }

            // Expected FOV: 80 * 0.85 = 68
            Assert.AreEqual(68.0f, cam.fieldOfView, 0.5f);

            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(swayGo);
        }

        [Test]
        public void FR15_MunitionsEconomy_ThreeConsecutiveHeadshots_WithoutDamage_AwardsRandomGadgetCharge()
        {
            var economyGo = new GameObject("TestEconomy");
            var economy = economyGo.AddComponent<MunitionsEconomyManager>();
            economy.SetInitialCharges(2, 2, 1);

            int initialTotalCharges = economy.BarricadeCharges + economy.SmokeCharges + economy.TripMineCharges;
            Assert.AreEqual(5, initialTotalCharges);

            var dummySentinel = economyGo.AddComponent<SentinelHealth>();

            // Kill 1: Headshot
            economy.HandleKill(dummySentinel, true, Vector3.zero);
            Assert.AreEqual(1, economy.CurrentHeadshotStreak);

            // Kill 2: Headshot
            economy.HandleKill(dummySentinel, true, Vector3.zero);
            Assert.AreEqual(2, economy.CurrentHeadshotStreak);

            // Kill 3: Headshot
            economy.HandleKill(dummySentinel, true, Vector3.zero);

            // Streak should reset to 0 after refund
            Assert.AreEqual(0, economy.CurrentHeadshotStreak);
            Assert.AreEqual(1, economy.TotalRefundsGranted);

            int newTotalCharges = economy.BarricadeCharges + economy.SmokeCharges + economy.TripMineCharges;
            Assert.AreEqual(initialTotalCharges + 1, newTotalCharges);

            // Verify damage resets streak
            economy.HandleKill(dummySentinel, true, Vector3.zero);
            Assert.AreEqual(1, economy.CurrentHeadshotStreak);
            economy.HandlePlayerDamage(10, Vector3.zero, false);
            Assert.AreEqual(0, economy.CurrentHeadshotStreak);

            Object.DestroyImmediate(economyGo);
        }

        [Test]
        public void FR9_FR11_FR12_BallisticsPenetrationCalculations()
        {
            // Barricade penetration: 25% damage penalty
            int normalDmg = BallisticsCalculator.CalculateDamage(45, false, 112, false);
            Assert.AreEqual(45, normalDmg);

            int barricadeDmg = BallisticsCalculator.CalculateDamage(45, false, 112, true);
            // 45 * 0.75 = 33.75 -> 34
            Assert.AreEqual(34, barricadeDmg);

            int headshotBarricadeDmg = BallisticsCalculator.CalculateDamage(45, true, 112, true);
            // 112 * 0.75 = 84
            Assert.AreEqual(84, headshotBarricadeDmg);
        }
    }
}
