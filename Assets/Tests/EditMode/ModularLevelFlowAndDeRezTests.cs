using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.Controller;
using NullProtocol.Combat;
using NullProtocol.World;
using NullProtocol.UI;

namespace NullProtocol.Tests.EditMode
{
    [TestFixture]
    public class ModularLevelFlowAndDeRezTests
    {
        private GameObject _testRootGo;

        [SetUp]
        public void SetUp()
        {
            _testRootGo = new GameObject("TestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRootGo != null)
            {
                Object.DestroyImmediate(_testRootGo);
            }
        }

        #region FR-28: Modular Grid & Calibrated Sightlines (4m Grid, 12m Max)

        [Test]
        public void FR28_ModularGrid_Constant_IsFourMeters()
        {
            Assert.AreEqual(4.0f, ModularGrid.GRID_SIZE, 0.001f);
        }

        [Test]
        public void FR28_MaxSightlineDistance_IsTwelveMeters()
        {
            Assert.AreEqual(12.0f, ModularGrid.MAX_SIGHTLINE_DISTANCE, 0.001f);
        }

        [Test]
        public void FR28_ModularGrid_Snap_SnapsToMultiplesOfFourMeters()
        {
            Vector3 rawPos = new Vector3(3.2f, 1.5f, 7.8f);
            Vector3 snapped = ModularGrid.Snap(rawPos);

            Assert.AreEqual(4.0f, snapped.x, 0.001f);
            Assert.AreEqual(1.5f, snapped.y, 0.001f); // Y unchanged by default
            Assert.AreEqual(8.0f, snapped.z, 0.001f);

            Vector3 snappedY = ModularGrid.Snap(rawPos, snapY: true);
            Assert.AreEqual(0.0f, snappedY.y, 0.001f); // 1.5 rounds to 0 (nearest multiple of 4)
        }

        [Test]
        public void FR28_ModularGrid_IsAligned_ValidatesStrictGridAdherence()
        {
            Vector3 aligned = new Vector3(8.0f, 0.0f, 16.0f);
            Vector3 unaligned = new Vector3(8.3f, 0.0f, 15.7f);

            Assert.IsTrue(ModularGrid.IsAligned(aligned, tolerance: 0.05f));
            Assert.IsFalse(ModularGrid.IsAligned(unaligned, tolerance: 0.05f));
        }

        [Test]
        public void FR28_ModularGridTile_SnapsPositionOnDemand()
        {
            var tileGo = new GameObject("TestTile");
            tileGo.transform.SetParent(_testRootGo.transform);
            tileGo.transform.position = new Vector3(5.1f, 0f, 10.9f);

            var tile = tileGo.AddComponent<ModularGridTile>();
            tile.SnapToModularGrid();

            Assert.AreEqual(4.0f, tileGo.transform.position.x, 0.001f);
            Assert.AreEqual(12.0f, tileGo.transform.position.z, 0.001f);
            Assert.IsTrue(tile.IsGridAligned());
        }

        [Test]
        public void FR28_RoomSightlineValidator_ValidatesCombatSightlinesWithinTwelveMeters()
        {
            Vector3 origin = Vector3.zero;
            Vector3 targetWithin12m = new Vector3(0f, 0f, 11.5f);
            Vector3 targetExceeding12m = new Vector3(0f, 0f, 16.0f);

            // Within 12m sightline is valid
            bool validClose = RoomSightlineValidator.IsSightlineCalibrated(origin, targetWithin12m, 0, out float closeDist);
            Assert.IsTrue(validClose);
            Assert.AreEqual(11.5f, closeDist, 0.01f);

            // Exceeding 12m without obstacle fails calibration
            bool validFar = RoomSightlineValidator.IsSightlineCalibrated(origin, targetExceeding12m, 0, out float farDist);
            Assert.IsFalse(validFar);
            Assert.AreEqual(16.0f, farDist, 0.01f);
        }

        #endregion

        #region FR-29: Campaign Levels & Subsector Select Flow

        [Test]
        public void FR29_Campaign_ContainsThreeDistinctLevels()
        {
            var campaignGo = new GameObject("CampaignFlowManager");
            campaignGo.transform.SetParent(_testRootGo.transform);
            var manager = campaignGo.AddComponent<CampaignFlowManager>();

            Assert.AreEqual(3, manager.TotalSubsectorsCount);

            var s1 = manager.GetSubsectorData(SubsectorId.Subsector01);
            var s2 = manager.GetSubsectorData(SubsectorId.Subsector02);
            var s3 = manager.GetSubsectorData(SubsectorId.RootCore);

            Assert.IsNotNull(s1);
            Assert.AreEqual("Subsector 01", s1.SubsectorTitle);
            StringAssert.Contains("Brutalist Corridors", s1.ThemeDescription);

            Assert.IsNotNull(s2);
            Assert.AreEqual("Subsector 02", s2.SubsectorTitle);
            StringAssert.Contains("Void Catwalks", s2.ThemeDescription);

            Assert.IsNotNull(s3);
            Assert.AreEqual("Root Core", s3.SubsectorTitle);
            StringAssert.Contains("Collapsing Arena", s3.ThemeDescription);
        }

        [Test]
        public void FR29_SubsectorSelectFlow_UnlocksLevelsSequentially()
        {
            var campaignGo = new GameObject("CampaignFlowManager");
            campaignGo.transform.SetParent(_testRootGo.transform);
            var manager = campaignGo.AddComponent<CampaignFlowManager>();

            // Subsector 01 starts unlocked, others locked
            Assert.IsTrue(manager.IsSubsectorUnlocked(SubsectorId.Subsector01));
            Assert.IsFalse(manager.IsSubsectorUnlocked(SubsectorId.Subsector02));
            Assert.IsFalse(manager.IsSubsectorUnlocked(SubsectorId.RootCore));

            // Cannot select locked Subsector 02
            bool selectedLocked = manager.SelectSubsector(SubsectorId.Subsector02);
            Assert.IsFalse(selectedLocked);

            // Complete Subsector 01 -> unlocks Subsector 02
            manager.SelectSubsector(SubsectorId.Subsector01);
            manager.CompleteCurrentSubsector();

            Assert.IsTrue(manager.IsSubsectorUnlocked(SubsectorId.Subsector02));
            Assert.IsFalse(manager.IsSubsectorUnlocked(SubsectorId.RootCore));

            // Complete Subsector 02 -> unlocks Root Core
            manager.SelectSubsector(SubsectorId.Subsector02);
            manager.CompleteCurrentSubsector();

            Assert.IsTrue(manager.IsSubsectorUnlocked(SubsectorId.RootCore));
        }

        #endregion

        #region FR-30: Instant Memory-Dump Loop (<= 2.0s without full scene reload)

        [Test]
        public void FR30_MemoryDumpManager_Duration_IsConstrainedUnderTwoSeconds()
        {
            var managerGo = new GameObject("MemoryDumpManager");
            managerGo.transform.SetParent(_testRootGo.transform);
            var manager = managerGo.AddComponent<MemoryDumpManager>();

            Assert.LessOrEqual(manager.ResetDuration, 2.0f);
        }

        [Test]
        public void FR30_RoomController_ResetsRegisteredEntitiesAndRepositionPlayer()
        {
            // Setup room
            var roomGo = new GameObject("Room_01");
            roomGo.transform.SetParent(_testRootGo.transform);
            var room = roomGo.AddComponent<RoomController>();

            var spawnPoint = new GameObject("SpawnPoint").transform;
            spawnPoint.SetParent(roomGo.transform);
            spawnPoint.position = new Vector3(4f, 0f, 4f);

            var spawnField = typeof(RoomController).GetField("_spawnPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            spawnField.SetValue(room, spawnPoint);

            // Setup mock resettable entity (e.g. enemy or barricade)
            var mockResettable = new MockResettable();
            room.RegisterResettable(mockResettable);

            // Setup player
            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(_testRootGo.transform);
            playerGo.transform.position = new Vector3(10f, 0f, 10f);
            var charController = playerGo.AddComponent<CharacterController>();
            var locomotion = playerGo.AddComponent<TacticalLocomotionController>();
            var health = playerGo.AddComponent<PlayerHealth>();

            // Damage player
            health.TakeDamage(50, Vector3.zero, Vector3.up, false);
            Assert.AreEqual(50, health.CurrentHealth);

            // Execute instant room reset
            room.ResetRoom(locomotion, health);

            // Verify: Resettables called, player repositioned to entrance, health restored
            Assert.IsTrue(mockResettable.WasResetCalled);
            Assert.AreEqual(100, health.CurrentHealth);
            Assert.AreEqual(4.0f, playerGo.transform.position.x, 0.01f);
            Assert.AreEqual(4.0f, playerGo.transform.position.z, 0.01f);
        }

        private class MockResettable : IResettable
        {
            public bool WasResetCalled { get; private set; }
            public void ResetState() => WasResetCalled = true;
        }

        #endregion

        #region FR-31: Level 3 Root Core Dynamic Tile De-Referencing (3 Waves, 40% Reduction)

        [Test]
        public void FR31_RootCoreDeReferencer_PartitionsTilesToFortyPercentReduction()
        {
            var deRezGo = new GameObject("RootCoreDeReferencer");
            deRezGo.transform.SetParent(_testRootGo.transform);
            var deRez = deRezGo.AddComponent<PerimeterTileDeReferencer>();

            // Create 100 arena tiles
            var tiles = new List<VoidFallTile>();
            for (int i = 0; i < 100; i++)
            {
                var tileGo = new GameObject($"Tile_{i}");
                tileGo.transform.SetParent(deRezGo.transform);
                var fallTile = tileGo.AddComponent<VoidFallTile>();
                tiles.Add(fallTile);
            }

            deRez.ConfigureArenaTiles(tiles);

            Assert.AreEqual(100, deRez.InitialTileCount);
            Assert.AreEqual(100, deRez.ActiveTileCount);
            Assert.AreEqual(0f, deRez.AreaReductionPercentage, 0.01f);

            // Execute Wave 1
            deRez.TriggerWaveImmediate(1);
            Assert.AreEqual(1, deRez.CurrentWave);
            Assert.Greater(deRez.AreaReductionPercentage, 0f);

            // Execute Wave 2
            deRez.TriggerWaveImmediate(2);
            Assert.AreEqual(2, deRez.CurrentWave);

            // Execute Wave 3
            deRez.TriggerWaveImmediate(3);
            Assert.AreEqual(3, deRez.CurrentWave);

            // Target FR-31: exactly 40% reduction across 3 waves
            Assert.AreEqual(40.0f, deRez.AreaReductionPercentage, 0.5f);
            Assert.AreEqual(60.0f, deRez.RemainingAreaPercentage, 0.5f);
            Assert.AreEqual(60, deRez.ActiveTileCount);

            // Reset arena back to full 100% playable area
            deRez.ResetState();
            Assert.AreEqual(100, deRez.ActiveTileCount);
            Assert.AreEqual(0f, deRez.AreaReductionPercentage, 0.01f);
        }

        #endregion

        #region FR-32: Data Core Terminals & Extraction Gateway (F Key Interaction)

        [Test]
        public void FR32_DataCoreTerminal_HackingUnlocksExtractionGateway()
        {
            var terminalGo = new GameObject("DataTerminal");
            terminalGo.transform.SetParent(_testRootGo.transform);
            var terminal = terminalGo.AddComponent<DataCoreTerminal>();

            var gatewayGo = new GameObject("ExtractionGateway");
            gatewayGo.transform.SetParent(_testRootGo.transform);
            var gateway = gatewayGo.AddComponent<ExtractionGateway>();

            gateway.RegisterRequiredTerminal(terminal);
            terminal.TargetGateway = gateway;

            // Gateway starts locked
            Assert.IsFalse(gateway.IsUnlocked);
            Assert.IsFalse(terminal.IsHacked);

            // Hack terminal
            terminal.CompleteHack();

            // Terminal is hacked and gateway is unlocked
            Assert.IsTrue(terminal.IsHacked);
            Assert.IsTrue(gateway.IsUnlocked);

            // Reset restores locked state
            terminal.ResetState();
            gateway.ResetState();
            Assert.IsFalse(terminal.IsHacked);
            Assert.IsFalse(gateway.IsUnlocked);
        }

        #endregion

        #region FR-33: Tactical Debrief Screen & Grade Calculation

        [Test]
        public void FR33_TacticalGradeCalculator_CalculatesGradesAccurately()
        {
            // Grade S: Low damage, high accuracy, within par time
            var gradeS = TacticalGradeCalculator.CalculateGrade(elapsedTime: 85f, accuracyPercentage: 85f, damageTaken: 15, parTimeSeconds: 90f);
            Assert.AreEqual(TacticalGrade.S, gradeS);

            // Grade S via flawless zero damage
            var gradeSFlawless = TacticalGradeCalculator.CalculateGrade(elapsedTime: 140f, accuracyPercentage: 65f, damageTaken: 0, parTimeSeconds: 90f);
            Assert.AreEqual(TacticalGrade.S, gradeSFlawless);

            // Grade A: Moderate damage, solid accuracy
            var gradeA = TacticalGradeCalculator.CalculateGrade(elapsedTime: 110f, accuracyPercentage: 55f, damageTaken: 45, parTimeSeconds: 90f);
            Assert.AreEqual(TacticalGrade.A, gradeA);

            // Grade B: Survivable damage, fair accuracy
            var gradeB = TacticalGradeCalculator.CalculateGrade(elapsedTime: 160f, accuracyPercentage: 35f, damageTaken: 85, parTimeSeconds: 90f);
            Assert.AreEqual(TacticalGrade.B, gradeB);

            // Grade C: Heavy damage taken / low accuracy
            var gradeC = TacticalGradeCalculator.CalculateGrade(elapsedTime: 200f, accuracyPercentage: 20f, damageTaken: 120, parTimeSeconds: 90f);
            Assert.AreEqual(TacticalGrade.C, gradeC);
        }

        [Test]
        public void FR33_TacticalDebriefData_FormatsCorrectly()
        {
            var data = new TacticalDebriefData(
                SubsectorId.Subsector01,
                "Subsector 01",
                elapsedTime: 75.42f,
                shotsFired: 20,
                shotsHit: 16,
                damageTaken: 25,
                TacticalGrade.S,
                enemiesNeutralized: 6
            );

            Assert.AreEqual("01:15.42", data.FormattedTime);
            Assert.AreEqual("80.0%", data.FormattedAccuracy);
            Assert.AreEqual("25 HP", data.FormattedDamage);
            Assert.AreEqual("S", data.GradeString);
        }

        [Test]
        public void FR33_LevelStatisticsTracker_GeneratesCompleteDebriefUponExtraction()
        {
            var trackerGo = new GameObject("StatsTracker");
            trackerGo.transform.SetParent(_testRootGo.transform);
            var tracker = trackerGo.AddComponent<LevelStatisticsTracker>();

            tracker.StartTracking();

            // Simulate 10 shots fired, 8 hit, 20 damage taken
            for (int i = 0; i < 10; i++) tracker.RegisterShotFired();
            for (int i = 0; i < 8; i++) tracker.RegisterShotHit();
            tracker.RegisterDamageTaken(20);

            Assert.AreEqual(10, tracker.ShotsFired);
            Assert.AreEqual(8, tracker.ShotsHit);
            Assert.AreEqual(80.0f, tracker.AccuracyPercentage, 0.01f);
            Assert.AreEqual(20, tracker.DamageTaken);

            var debrief = tracker.GenerateDebriefData();
            Assert.AreEqual(TacticalGrade.S, debrief.Grade);
            Assert.AreEqual(80.0f, debrief.AccuracyPercentage, 0.01f);
            Assert.AreEqual(20, debrief.DamageTaken);
        }

        #endregion
    }
}
