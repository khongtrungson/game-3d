using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.Controller;
using NullProtocol.Gadgets;
using NullProtocol.Persistence;
using NullProtocol.UI;
using NullProtocol.World;

namespace NullProtocol.Tests.EditMode
{
    [TestFixture]
    public class SessionPersistenceProfileAndSteamworksTests
    {
        private GameObject _testRoot;
        private string _tempTestDir;
        private ProfileStorageService _storageService;
        private MockSteamCloudStorage _mockCloudStorage;
        private MockSteamworksAchievementService _mockAchievements;

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TestRoot_Persistence");
            _tempTestDir = Path.Combine(Path.GetTempPath(), "NullProtocol_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempTestDir);

            _storageService = new ProfileStorageService(_tempTestDir);
            _mockCloudStorage = new MockSteamCloudStorage();
            _mockAchievements = new MockSteamworksAchievementService();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(_testRoot);
            }

            try
            {
                if (Directory.Exists(_tempTestDir))
                {
                    Directory.Delete(_tempTestDir, true);
                }
            }
            catch
            {
                // Ignored in test cleanup
            }
        }

        #region FR-40: Local JSON Persistence & SHA-256 Checksum (profile.json)

        [Test]
        public void FR40_ProfileStorage_DefaultFileName_IsProfileJson()
        {
            Assert.AreEqual("profile.json", ProfileStorageService.PROFILE_FILE_NAME);
            Assert.IsTrue(_storageService.SaveFilePath.EndsWith("profile.json"));
        }

        [Test]
        public void FR40_ProfileStorage_SaveAndLoad_PreservesFullProfileData()
        {
            var profile = new ProfileData
            {
                ProfileId = "Agent_404",
                CurrentSubsector = SubsectorId.Subsector02
            };
            profile.UnlockSubsector(SubsectorId.Subsector02);

            var debrief = new TacticalDebriefData(
                SubsectorId.Subsector01,
                "Subsector 01",
                45.5f,
                30,
                26,
                15,
                TacticalGrade.A,
                enemiesNeutralized: 8,
                headshots: 6
            );

            profile.GetRecord(SubsectorId.Subsector01)?.RecordCompletion(debrief);
            profile.Metrics.TotalShotsFired = 30;
            profile.Metrics.TotalShotsHit = 26;
            profile.Metrics.TotalHeadshots = 6;
            profile.Metrics.TotalDamageTaken = 15;

            bool saveResult = _storageService.Save(profile);
            Assert.IsTrue(saveResult, "Saving ProfileData must return true.");
            Assert.IsTrue(_storageService.Exists(), "profile.json must exist on disk.");

            // Load and assert identity
            var loaded = _storageService.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual("Agent_404", loaded.ProfileId);
            Assert.AreEqual(SubsectorId.Subsector02, loaded.CurrentSubsector);
            Assert.IsTrue(loaded.IsSubsectorUnlocked(SubsectorId.Subsector02));

            var loadedRecord = loaded.GetRecord(SubsectorId.Subsector01);
            Assert.IsNotNull(loadedRecord);
            Assert.IsTrue(loadedRecord.IsCompleted);
            Assert.AreEqual(TacticalGrade.A, loadedRecord.BestGrade);
            Assert.AreEqual(45.5f, loadedRecord.BestTimeSeconds, 0.01f);
            Assert.AreEqual(15, loadedRecord.LeastDamageTaken);
            Assert.AreEqual(30, loaded.Metrics.TotalShotsFired);
            Assert.AreEqual(26, loaded.Metrics.TotalShotsHit);
            Assert.AreEqual(6, loaded.Metrics.TotalHeadshots);
        }

        [Test]
        public void FR40_ProfileStorage_GeneratesValidSHA256ChecksumEnvelope()
        {
            var profile = new ProfileData { ProfileId = "IntegrityCheckAgent" };
            _storageService.Save(profile);

            string fileContent = File.ReadAllText(_storageService.SaveFilePath);
            Assert.IsTrue(fileContent.Contains("\"Checksum\":"), "Saved file must contain Checksum in envelope.");
            Assert.IsTrue(fileContent.Contains("\"PayloadJson\":"), "Saved file must contain PayloadJson in envelope.");

            var envelope = JsonUtility.FromJson<ProfileEnvelope>(fileContent);
            Assert.IsNotNull(envelope);
            Assert.IsFalse(string.IsNullOrEmpty(envelope.Checksum));
            Assert.IsTrue(envelope.TryUnpack(out var unpacked, verifyChecksum: true));
            Assert.AreEqual("IntegrityCheckAgent", unpacked.ProfileId);
        }

        [Test]
        public void FR40_ProfileStorage_DetectsCorruptedPayload()
        {
            var profile = new ProfileData { ProfileId = "UntamperedAgent" };
            _storageService.Save(profile);

            string raw = File.ReadAllText(_storageService.SaveFilePath);
            var envelope = JsonUtility.FromJson<ProfileEnvelope>(raw);

            // Tamper with payload while keeping original checksum
            envelope.PayloadJson = envelope.PayloadJson.Replace("UntamperedAgent", "TamperedHacker");
            string tamperedFileContent = JsonUtility.ToJson(envelope, true);
            File.WriteAllText(_storageService.SaveFilePath, tamperedFileContent);

            // TryUnpack should fail checksum validation
            bool success = envelope.TryUnpack(out var unpacked, verifyChecksum: true);
            Assert.IsFalse(success, "Tampered payload must fail checksum verification.");
        }

        [Test]
        public void FR40_ProfileStorage_MissingFile_InitializesDefaultProfile()
        {
            _storageService.Delete();
            Assert.IsFalse(_storageService.Exists());

            var profile = _storageService.Load();
            Assert.IsNotNull(profile);
            Assert.AreEqual(SubsectorId.Subsector01, profile.CurrentSubsector);
            Assert.IsTrue(profile.IsSubsectorUnlocked(SubsectorId.Subsector01));
            Assert.IsTrue(_storageService.Exists(), "Loading non-existent profile should write default profile to disk.");
        }

        [Test]
        public void FR40_ProfileStorage_Delete_RemovesFileFromDisk()
        {
            _storageService.Save(new ProfileData());
            Assert.IsTrue(_storageService.Exists());

            bool deleted = _storageService.Delete();
            Assert.IsTrue(deleted);
            Assert.IsFalse(_storageService.Exists());
        }

        #endregion

        #region FR-41: Automatic Checkpoint Disk Commits

        [Test]
        public void FR41_EnteringUnclearedRoom_AutomaticallyCommitsCheckpointToDisk()
        {
            var profile = new ProfileData();
            _storageService.Save(profile);

            var checkpointMgr = new CheckpointManager(profile, _storageService);

            var roomGo = new GameObject("Room_02");
            roomGo.transform.SetParent(_testRoot.transform);
            var room = roomGo.AddComponent<RoomController>();
            room.Initialize("Room_02", "Room_02");
            roomGo.transform.position = new Vector3(12f, 0f, 24f);

            Assert.IsFalse(room.IsCleared, "Room must initially be uncleared.");

            // Commit checkpoint on entering uncleared room (FR-41)
            bool committed = checkpointMgr.TryCommitRoomCheckpoint(room, SubsectorId.Subsector01);
            Assert.IsTrue(committed, "Checkpoint commit must return true for uncleared room.");
            Assert.IsTrue(checkpointMgr.HasActiveCheckpoint, "Active checkpoint must be established.");
            Assert.AreEqual("Room_02", checkpointMgr.ActiveCheckpoint.RoomDisplayName);
            Assert.AreEqual(new Vector3(12f, 0f, 24f), checkpointMgr.ActiveCheckpoint.PlayerPosition);

            // Verify written to disk
            var diskProfile = _storageService.Load();
            Assert.IsTrue(diskProfile.ActiveCheckpoint.HasActiveCheckpoint, "Disk profile must contain active checkpoint.");
            Assert.AreEqual("Room_02", diskProfile.ActiveCheckpoint.RoomDisplayName);
        }

        [Test]
        public void FR41_EnteringClearedRoom_DoesNotCommitCheckpoint()
        {
            var profile = new ProfileData();
            _storageService.Save(profile);

            var checkpointMgr = new CheckpointManager(profile, _storageService);

            var roomGo = new GameObject("ClearedRoom");
            roomGo.transform.SetParent(_testRoot.transform);
            var room = roomGo.AddComponent<RoomController>();
            room.SetRoomCleared();
            Assert.IsTrue(room.IsCleared);

            bool committed = checkpointMgr.TryCommitRoomCheckpoint(room, SubsectorId.Subsector01);
            Assert.IsFalse(committed, "Cleared room must not commit a new checkpoint to disk (FR-41).");
            Assert.IsFalse(checkpointMgr.HasActiveCheckpoint);
        }

        [Test]
        public void FR41_LevelCompletion_CommitsProgressionAndClearsActiveCheckpointOnDisk()
        {
            var profile = new ProfileData();
            var checkpointMgr = new CheckpointManager(profile, _storageService);

            // Establish active mid-level checkpoint first
            var roomGo = new GameObject("MidRoom");
            roomGo.transform.SetParent(_testRoot.transform);
            var room = roomGo.AddComponent<RoomController>();
            checkpointMgr.TryCommitRoomCheckpoint(room, SubsectorId.Subsector01);
            Assert.IsTrue(checkpointMgr.HasActiveCheckpoint);

            // Level completion occurs
            var debrief = new TacticalDebriefData(
                SubsectorId.Subsector01,
                "Subsector 01",
                60.0f,
                40,
                35,
                0,
                TacticalGrade.S,
                enemiesNeutralized: 10,
                headshots: 10
            );

            bool committed = checkpointMgr.CommitLevelCompletion(SubsectorId.Subsector01, debrief);
            Assert.IsTrue(committed, "Level completion commit must succeed.");
            Assert.IsFalse(checkpointMgr.HasActiveCheckpoint, "Active mid-level checkpoint must be cleared upon level completion.");

            // Check progression in profile and on disk
            Assert.IsTrue(profile.IsSubsectorUnlocked(SubsectorId.Subsector02), "Subsector 02 must be unlocked.");
            Assert.AreEqual(SubsectorId.Subsector02, profile.CurrentSubsector);

            var diskProfile = _storageService.Load();
            Assert.IsFalse(diskProfile.ActiveCheckpoint.HasActiveCheckpoint);
            Assert.IsTrue(diskProfile.IsSubsectorUnlocked(SubsectorId.Subsector02));
            var record = diskProfile.GetRecord(SubsectorId.Subsector01);
            Assert.IsTrue(record.IsCompleted);
            Assert.AreEqual(TacticalGrade.S, record.BestGrade);
        }

        [Test]
        public void FR41_RestoreCheckpoint_RestoresPlayerPositionAndState()
        {
            var profile = new ProfileData();
            var checkpointMgr = new CheckpointManager(profile, _storageService);

            var roomGo = new GameObject("CheckpointRoom");
            roomGo.transform.SetParent(_testRoot.transform);
            roomGo.transform.position = new Vector3(8f, 0f, 16f);
            var room = roomGo.AddComponent<RoomController>();

            checkpointMgr.TryCommitRoomCheckpoint(room, SubsectorId.Subsector01);

            // Setup player
            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(_testRoot.transform);
            playerGo.transform.position = Vector3.zero;
            var locomotion = playerGo.AddComponent<TacticalLocomotionController>();
            var health = playerGo.AddComponent<PlayerHealth>();

            bool restored = checkpointMgr.RestoreCheckpoint(locomotion, health);
            Assert.IsTrue(restored);
            Assert.AreEqual(new Vector3(8f, 0f, 16f), playerGo.transform.position);
            Assert.AreEqual(100, health.CurrentHealth);
        }

        #endregion

        #region FR-42: Steam Cloud Synchronization

        [Test]
        public void FR42_SteamCloud_PushToCloud_UploadsLocalProfile()
        {
            var profile = new ProfileData { ProfileId = "CloudPushAgent" };
            _storageService.Save(profile);

            var syncManager = new SteamCloudSyncManager(_storageService, _mockCloudStorage);
            var result = syncManager.PushToCloud();

            Assert.AreEqual(CloudSyncResult.UploadedToCloud, result);
            Assert.IsTrue(_mockCloudStorage.FileExists(ProfileStorageService.PROFILE_FILE_NAME));

            byte[] cloudBytes = _mockCloudStorage.FileRead(ProfileStorageService.PROFILE_FILE_NAME);
            Assert.IsNotNull(cloudBytes);
            Assert.Greater(cloudBytes.Length, 0);
        }

        [Test]
        public void FR42_SteamCloud_PullFromCloud_DownloadsRemoteProfile()
        {
            // Seed cloud storage
            var remoteProfile = new ProfileData { ProfileId = "RemoteCloudAgent" };
            var envelope = ProfileEnvelope.Create(remoteProfile);
            byte[] remoteBytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope, true));
            _mockCloudStorage.FileWrite(ProfileStorageService.PROFILE_FILE_NAME, remoteBytes);

            // Local file does not exist initially
            _storageService.Delete();
            Assert.IsFalse(_storageService.Exists());

            var syncManager = new SteamCloudSyncManager(_storageService, _mockCloudStorage);
            var result = syncManager.PullFromCloud();

            Assert.AreEqual(CloudSyncResult.DownloadedFromCloud, result);
            Assert.IsTrue(_storageService.Exists(), "Local file should be populated from Steam Cloud.");

            var loaded = _storageService.Load();
            Assert.AreEqual("RemoteCloudAgent", loaded.ProfileId);
        }

        [Test]
        public void FR42_SteamCloud_SyncManager_ResolvesConflict_WhenCloudIsNewer()
        {
            // Local file saved earlier
            var localProfile = new ProfileData { ProfileId = "OldLocalAgent" };
            _storageService.Save(localProfile);
            File.SetLastWriteTimeUtc(_storageService.SaveFilePath, DateTime.UtcNow.AddMinutes(-10));

            // Cloud file saved recently
            var cloudProfile = new ProfileData { ProfileId = "NewCloudAgent" };
            var envelope = ProfileEnvelope.Create(cloudProfile);
            byte[] cloudBytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope, true));
            _mockCloudStorage.FileWrite(ProfileStorageService.PROFILE_FILE_NAME, cloudBytes);
            _mockCloudStorage.SetFileTimestamp(ProfileStorageService.PROFILE_FILE_NAME, DateTime.UtcNow);

            var syncManager = new SteamCloudSyncManager(_storageService, _mockCloudStorage);
            var syncResult = syncManager.Synchronize();

            Assert.AreEqual(CloudSyncResult.DownloadedFromCloud, syncResult);
            var loaded = _storageService.Load();
            Assert.AreEqual("NewCloudAgent", loaded.ProfileId);
        }

        [Test]
        public void FR42_SteamCloud_SyncManager_ResolvesConflict_WhenLocalIsNewer()
        {
            // Cloud file saved earlier
            var cloudProfile = new ProfileData { ProfileId = "OldCloudAgent" };
            var envelope = ProfileEnvelope.Create(cloudProfile);
            byte[] cloudBytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope, true));
            _mockCloudStorage.FileWrite(ProfileStorageService.PROFILE_FILE_NAME, cloudBytes);
            _mockCloudStorage.SetFileTimestamp(ProfileStorageService.PROFILE_FILE_NAME, DateTime.UtcNow.AddMinutes(-10));

            // Local file saved recently
            var localProfile = new ProfileData { ProfileId = "NewLocalAgent" };
            _storageService.Save(localProfile);
            File.SetLastWriteTimeUtc(_storageService.SaveFilePath, DateTime.UtcNow);

            var syncManager = new SteamCloudSyncManager(_storageService, _mockCloudStorage);
            var syncResult = syncManager.Synchronize();

            Assert.AreEqual(CloudSyncResult.UploadedToCloud, syncResult);
            byte[] updatedCloudBytes = _mockCloudStorage.FileRead(ProfileStorageService.PROFILE_FILE_NAME);
            string updatedJson = System.Text.Encoding.UTF8.GetString(updatedCloudBytes);
            Assert.IsTrue(updatedJson.Contains("NewLocalAgent"));
        }

        [Test]
        public void FR42_SteamCloud_GracefulHandling_WhenCloudUnavailable()
        {
            _mockCloudStorage.IsAvailable = false;

            var syncManager = new SteamCloudSyncManager(_storageService, _mockCloudStorage);
            var result = syncManager.Synchronize();

            Assert.AreEqual(CloudSyncResult.CloudUnavailable, result);
        }

        #endregion

        #region FR-43: 10 Core Steam Achievements

        [Test]
        public void FR43_AchievementDatabase_ContainsAllTenCoreAchievements()
        {
            Assert.AreEqual(10, AchievementDatabase.TotalAchievementsCount, "Must define exactly 10 core achievements (FR-43).");

            // Campaign Milestones
            Assert.IsNotNull(AchievementDatabase.Get(AchievementDatabase.ACH_SUBSECTOR_01_CLEAR));
            Assert.AreEqual("Subsector Purged", AchievementDatabase.Get(AchievementDatabase.ACH_SUBSECTOR_01_CLEAR).Title);

            Assert.IsNotNull(AchievementDatabase.Get(AchievementDatabase.ACH_SUBSECTOR_02_CLEAR));
            Assert.AreEqual("Archive Purged", AchievementDatabase.Get(AchievementDatabase.ACH_SUBSECTOR_02_CLEAR).Title);

            Assert.IsNotNull(AchievementDatabase.Get(AchievementDatabase.ACH_ROOT_CORE_CLEAR));
            Assert.AreEqual("Root Core Dereferenced", AchievementDatabase.Get(AchievementDatabase.ACH_ROOT_CORE_CLEAR).Title);

            // Tactical Mastery
            Assert.IsNotNull(AchievementDatabase.Get(AchievementDatabase.ACH_CLEAN_SWEEP));
            Assert.AreEqual("Clean Sweep", AchievementDatabase.Get(AchievementDatabase.ACH_CLEAN_SWEEP).Title);

            Assert.IsNotNull(AchievementDatabase.Get(AchievementDatabase.ACH_SURGICAL_EXECUTION));
            Assert.AreEqual("Surgical Execution", AchievementDatabase.Get(AchievementDatabase.ACH_SURGICAL_EXECUTION).Title);

            Assert.IsNotNull(AchievementDatabase.Get(AchievementDatabase.ACH_GHOST_CLEAR));
            Assert.AreEqual("Ghost in the System", AchievementDatabase.Get(AchievementDatabase.ACH_GHOST_CLEAR).Title);

            Assert.IsNotNull(AchievementDatabase.Get(AchievementDatabase.ACH_UNTOUCHABLE));
            Assert.AreEqual("Untouchable", AchievementDatabase.Get(AchievementDatabase.ACH_UNTOUCHABLE).Title);

            // Gadget Proficiency
            Assert.IsNotNull(AchievementDatabase.Get(AchievementDatabase.ACH_GADGET_BARRICADE));
            Assert.AreEqual("Hard-Light Architect", AchievementDatabase.Get(AchievementDatabase.ACH_GADGET_BARRICADE).Title);

            Assert.IsNotNull(AchievementDatabase.Get(AchievementDatabase.ACH_GADGET_SMOKE));
            Assert.AreEqual("Sensor Blind", AchievementDatabase.Get(AchievementDatabase.ACH_GADGET_SMOKE).Title);

            Assert.IsNotNull(AchievementDatabase.Get(AchievementDatabase.ACH_GADGET_TRIPMINE));
            Assert.AreEqual("Logic Frozen", AchievementDatabase.Get(AchievementDatabase.ACH_GADGET_TRIPMINE).Title);
        }

        [Test]
        public void FR43_CampaignMilestones_UnlockUponSubsectorCompletion()
        {
            var profile = new ProfileData();
            var tracker = new AchievementTracker(profile, _mockAchievements);

            // Subsector 01 completion
            var debrief01 = new TacticalDebriefData(SubsectorId.Subsector01, "SEC-01", 50f, 20, 15, 30, TacticalGrade.B);
            tracker.EvaluateDebriefAchievements(debrief01);

            Assert.IsTrue(tracker.IsUnlocked(AchievementDatabase.ACH_SUBSECTOR_01_CLEAR));
            Assert.IsTrue(_mockAchievements.IsAchievementUnlocked(AchievementDatabase.ACH_SUBSECTOR_01_CLEAR));

            // Subsector 02 completion
            var debrief02 = new TacticalDebriefData(SubsectorId.Subsector02, "SEC-02", 90f, 30, 25, 40, TacticalGrade.B);
            tracker.EvaluateDebriefAchievements(debrief02);

            Assert.IsTrue(tracker.IsUnlocked(AchievementDatabase.ACH_SUBSECTOR_02_CLEAR));

            // Root Core completion
            var debrief03 = new TacticalDebriefData(SubsectorId.RootCore, "SEC-03", 120f, 40, 35, 50, TacticalGrade.B);
            tracker.EvaluateDebriefAchievements(debrief03);

            Assert.IsTrue(tracker.IsUnlocked(AchievementDatabase.ACH_ROOT_CORE_CLEAR));
        }

        [Test]
        public void FR43_TacticalMastery_CleanSweep_UnlocksOnZeroDamageRoomClear()
        {
            var profile = new ProfileData();
            var tracker = new AchievementTracker(profile, _mockAchievements);

            var roomGo = new GameObject("CombatRoom");
            roomGo.transform.SetParent(_testRoot.transform);
            var room = roomGo.AddComponent<RoomController>();

            tracker.HandleRoomEntered(room);
            // Player takes NO damage
            tracker.HandleRoomCleared(room);

            Assert.IsTrue(tracker.IsUnlocked(AchievementDatabase.ACH_CLEAN_SWEEP), "Zero damage room clear must unlock Clean Sweep (FR-43).");
            Assert.AreEqual(1, profile.Metrics.TotalZeroDamageRoomsCleared);
        }

        [Test]
        public void FR43_TacticalMastery_CleanSweep_DoesNotUnlockWhenDamageTaken()
        {
            var profile = new ProfileData();
            var tracker = new AchievementTracker(profile, _mockAchievements);

            var roomGo = new GameObject("CombatRoomDamaged");
            roomGo.transform.SetParent(_testRoot.transform);
            var room = roomGo.AddComponent<RoomController>();

            tracker.HandleRoomEntered(room);
            tracker.HandlePlayerDamaged(25); // Took 25 damage
            tracker.HandleRoomCleared(room);

            Assert.IsFalse(tracker.IsUnlocked(AchievementDatabase.ACH_CLEAN_SWEEP), "Room clear with damage must not unlock Clean Sweep.");
            Assert.AreEqual(0, profile.Metrics.TotalZeroDamageRoomsCleared);
        }

        [Test]
        public void FR43_TacticalMastery_SurgicalExecution_UnlocksAtEightyPercentHeadshotRate()
        {
            var profile = new ProfileData();
            var tracker = new AchievementTracker(profile, _mockAchievements);

            // 10 hits, 9 headshots -> 90% headshot rate
            var debrief = new TacticalDebriefData(
                SubsectorId.Subsector01,
                "Subsector 01",
                45f,
                10,
                10,
                10,
                TacticalGrade.A,
                enemiesNeutralized: 9,
                headshots: 9
            );

            tracker.EvaluateDebriefAchievements(debrief);

            Assert.IsTrue(tracker.IsUnlocked(AchievementDatabase.ACH_SURGICAL_EXECUTION), "80%+ headshot rate must unlock Surgical Execution (FR-43).");
        }

        [Test]
        public void FR43_TacticalMastery_GhostInTheSystem_UnlocksOnGradeS()
        {
            var profile = new ProfileData();
            var tracker = new AchievementTracker(profile, _mockAchievements);

            var debrief = new TacticalDebriefData(
                SubsectorId.Subsector01,
                "Subsector 01",
                40f,
                20,
                18,
                10,
                TacticalGrade.S
            );

            tracker.EvaluateDebriefAchievements(debrief);

            Assert.IsTrue(tracker.IsUnlocked(AchievementDatabase.ACH_GHOST_CLEAR), "Grade S debrief must unlock Ghost in the System (FR-43).");
        }

        [Test]
        public void FR43_TacticalMastery_Untouchable_UnlocksOnZeroDamageRun()
        {
            var profile = new ProfileData();
            var tracker = new AchievementTracker(profile, _mockAchievements);

            var debrief = new TacticalDebriefData(
                SubsectorId.Subsector01,
                "Subsector 01",
                55f,
                25,
                20,
                0, // 0 damage taken
                TacticalGrade.S
            );

            tracker.EvaluateDebriefAchievements(debrief);

            Assert.IsTrue(tracker.IsUnlocked(AchievementDatabase.ACH_UNTOUCHABLE), "Zero damage subsector run must unlock Untouchable (FR-43).");
        }

        [Test]
        public void FR43_GadgetProficiency_HardLightArchitect_UnlocksAtTenBarricades()
        {
            var profile = new ProfileData();
            var tracker = new AchievementTracker(profile, _mockAchievements);

            for (int i = 0; i < 9; i++)
            {
                tracker.HandleGadgetUsed(GadgetType.HardLightBarricade, 1);
            }
            Assert.IsFalse(tracker.IsUnlocked(AchievementDatabase.ACH_GADGET_BARRICADE));

            // 10th barricade
            tracker.HandleGadgetUsed(GadgetType.HardLightBarricade, 0);
            Assert.IsTrue(tracker.IsUnlocked(AchievementDatabase.ACH_GADGET_BARRICADE), "10 barricades deployed must unlock Hard-Light Architect (FR-43).");
            Assert.AreEqual(10, profile.Metrics.TotalBarricadesDeployed);
        }

        [Test]
        public void FR43_GadgetProficiency_SensorBlind_UnlocksAtTenSmokes()
        {
            var profile = new ProfileData();
            var tracker = new AchievementTracker(profile, _mockAchievements);

            for (int i = 0; i < 9; i++)
            {
                tracker.HandleGadgetUsed(GadgetType.NullCloudSmoke, 1);
            }
            Assert.IsFalse(tracker.IsUnlocked(AchievementDatabase.ACH_GADGET_SMOKE));

            // 10th smoke
            tracker.HandleGadgetUsed(GadgetType.NullCloudSmoke, 0);
            Assert.IsTrue(tracker.IsUnlocked(AchievementDatabase.ACH_GADGET_SMOKE), "10 smokes deployed must unlock Sensor Blind (FR-43).");
            Assert.AreEqual(10, profile.Metrics.TotalSmokesDeployed);
        }

        [Test]
        public void FR43_GadgetProficiency_LogicFrozen_UnlocksAtFiveFrozenHostiles()
        {
            var profile = new ProfileData();
            var tracker = new AchievementTracker(profile, _mockAchievements);

            for (int i = 0; i < 4; i++)
            {
                tracker.HandleHostileFrozenByMine();
            }
            Assert.IsFalse(tracker.IsUnlocked(AchievementDatabase.ACH_GADGET_TRIPMINE));

            // 5th frozen hostile
            tracker.HandleHostileFrozenByMine();
            Assert.IsTrue(tracker.IsUnlocked(AchievementDatabase.ACH_GADGET_TRIPMINE), "5 frozen hostiles must unlock Logic Frozen (FR-43).");
            Assert.AreEqual(5, profile.Metrics.TotalTripMinesFrozen);
        }

        [Test]
        public void FR43_PersistenceManager_IntegratesStorageCloudAndAchievements()
        {
            var pmGo = new GameObject("PersistenceManager");
            pmGo.transform.SetParent(_testRoot.transform);
            var pm = pmGo.AddComponent<PersistenceManager>();

            pm.InitializeServices(_storageService, _mockCloudStorage, _mockAchievements);

            Assert.IsNotNull(pm.CurrentProfile);
            Assert.IsNotNull(pm.Achievements);
            Assert.IsNotNull(pm.Checkpoints);

            // Test room entry trigger
            var roomGo = new GameObject("TestRoom");
            roomGo.transform.SetParent(_testRoot.transform);
            var room = roomGo.AddComponent<RoomController>();

            pm.OnPlayerEnteredRoom(room, SubsectorId.Subsector01);
            Assert.IsTrue(pm.Checkpoints.HasActiveCheckpoint);

            // Test level completion trigger
            var debrief = new TacticalDebriefData(
                SubsectorId.Subsector01,
                "SEC-01",
                50f,
                20,
                18,
                0,
                TacticalGrade.S,
                enemiesNeutralized: 8,
                headshots: 8
            );

            pm.OnLevelCompleted(SubsectorId.Subsector01, debrief);

            Assert.IsTrue(pm.Achievements.IsUnlocked(AchievementDatabase.ACH_SUBSECTOR_01_CLEAR));
            Assert.IsTrue(pm.Achievements.IsUnlocked(AchievementDatabase.ACH_UNTOUCHABLE));
            Assert.IsTrue(pm.Achievements.IsUnlocked(AchievementDatabase.ACH_GHOST_CLEAR));
            Assert.IsTrue(pm.Achievements.IsUnlocked(AchievementDatabase.ACH_SURGICAL_EXECUTION));

            // Verify disk persistence
            var diskProfile = _storageService.Load();
            Assert.IsTrue(diskProfile.IsAchievementUnlocked(AchievementDatabase.ACH_SUBSECTOR_01_CLEAR));
            Assert.IsTrue(diskProfile.IsAchievementUnlocked(AchievementDatabase.ACH_UNTOUCHABLE));
            Assert.IsTrue(diskProfile.IsAchievementUnlocked(AchievementDatabase.ACH_GHOST_CLEAR));
            Assert.IsTrue(diskProfile.IsAchievementUnlocked(AchievementDatabase.ACH_SURGICAL_EXECUTION));

            // Verify cloud sync
            Assert.IsTrue(_mockCloudStorage.FileExists(ProfileStorageService.PROFILE_FILE_NAME));
        }

        #endregion
    }
}
