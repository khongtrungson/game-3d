using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using NullProtocol.Core;
using NullProtocol.Controller;
using NullProtocol.Combat;
using NullProtocol.Gadgets;
using NullProtocol.AI;
using NullProtocol.World;
using NullProtocol.UI;

namespace NullProtocol.Editor
{
    /// <summary>
    /// Builder for Epic 7:
    /// - Level 2: Subsector 02 (Corrupted Archives) - Vertical catwalks over infinite void, high-elevation snipers, server banks.
    /// - Level 3: Root Core (Collapsing Arena) - Hexagonal tiles de-referencing across 3 waves (40% area drop), Corrupted Monolith, and Null-01 Mirror Operative Boss Duel.
    /// </summary>
    public static class Epic7SceneBuilder
    {
        private const string SCENE_LEVEL2_PATH = "Assets/Scenes/Subsector02_CorruptedArchives.unity";
        private const string SCENE_LEVEL3_PATH = "Assets/Scenes/Subsector03_RootCore.unity";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";
        private const string EVENT_CHANNELS_FOLDER = "Assets/_Project/ScriptableObjects/Events";
        private const string WEAPON_DATA_FOLDER = "Assets/_Project/ScriptableObjects/Weapons";

        // Asset Paths for 3D Models
        private const string PATH_FLOOR = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_01_FloorTile.glb";
        private const string PATH_WALL = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_02_SolidWall.glb";
        private const string PATH_DOORFRAME = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_03_WallDoorframe.glb";
        private const string PATH_COVER_PILLAR = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_05_CoverPillar.glb";
        private const string PATH_LOW_COVER = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_06_LowCoverHalfWall.glb";
        private const string PATH_THRESHOLD = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_15_ThresholdMarker.glb";
        private const string PATH_TERMINAL = "Assets/Art/Environment/Props/ENV_PROP_01_DataCoreTerminal.glb";
        private const string PATH_GATEWAY = "Assets/Art/Environment/Props/ENV_PROP_02_ExtractionGateway.glb";

        // Subsector 02 Specific Assets
        private const string PATH_CATWALK_STRAIGHT = "Assets/Art/Environment/Subsector02_CorruptedArchives/ENV_MOD_08_CatwalkStraight.glb";
        private const string PATH_CATWALK_STAIRS = "Assets/Art/Environment/Subsector02_CorruptedArchives/ENV_MOD_09_CatwalkStairs.glb";
        private const string PATH_CATWALK_RAILING = "Assets/Art/Environment/Subsector02_CorruptedArchives/ENV_MOD_10_CatwalkRailing.glb";
        private const string PATH_VOID_PYLON = "Assets/Art/Environment/Subsector02_CorruptedArchives/ENV_MOD_11_VoidPylon.glb";
        private const string PATH_SERVER_RACK = "Assets/Art/Environment/Subsector02_CorruptedArchives/ENV_MOD_12_ServerRackBank.glb";
        private const string PATH_SERVER_DEBRIS = "Assets/Art/Environment/Subsector02_CorruptedArchives/ENV_PROP_04_ServerDebris.glb";

        // Subsector 03 Specific Assets
        private const string PATH_HEX_TILE = "Assets/Art/Environment/Subsector03_RootCore/ENV_MOD_14_CollapsingHexTile.glb";
        private const string PATH_MONOLITH = "Assets/Art/Environment/Subsector03_RootCore/ENV_PROP_03_CorruptedMonolith.glb";

        // Characters & Weapons
        private const string PATH_FP_ARMS = "Assets/Art/Characters/FirstPersonViewmodel/CHR_FP_ARMS.glb";
        private const string PATH_OLED_WATCH = "Assets/Art/Characters/CHR_OLED_WATCH.glb";
        private const string PATH_SYNAPSE_AR = "Assets/Art/Weapons/SynapseAR/WEP_MOD_02_SynapseAR.glb";
        private const string PATH_VECTOR9 = "Assets/Art/Weapons/Vector9/WEP_MOD_01_Vector9.glb";
        private const string PATH_SENTINEL = "Assets/Art/Characters/Enemies/Sentinel/CHR_AI_SENTINEL.glb";
        private const string PATH_BREACHER = "Assets/Art/Characters/Enemies/ShieldBreacher/CHR_AI_BREACHER.glb";
        private const string PATH_BREACHER_SHIELD = "Assets/Art/Characters/Enemies/ShieldBreacher/CHR_AI_BREACHER_SHIELD.glb";
        private const string PATH_NULL01_BOSS = "Assets/Art/Characters/Enemies/Null01_Boss/CHR_AI_NULL01.glb";

        // Gadgets & VFX
        private const string PATH_GAD_PUCK = "Assets/Art/Gadgets/HardLightBarricade/GAD_MOD_01_PUCK.glb";
        private const string PATH_GAD_BARRIER = "Assets/Art/Gadgets/HardLightBarricade/GAD_MOD_01_BARRIER.glb";
        private const string PATH_GAD_CANISTER = "Assets/Art/Gadgets/NullCloudSmoke/GAD_MOD_02_CANISTER.glb";
        private const string PATH_GAD_MINE = "Assets/Art/Gadgets/LogicTripMine/GAD_MOD_03_MINE.glb";

        private const string PATH_VFX_BARRICADE_DEPLOY = "Assets/VFX/Gadgets/VFX_Barricade_Deploy.prefab";
        private const string PATH_VFX_BARRICADE_SHATTER = "Assets/VFX/Gadgets/VFX_Barricade_Shatter.prefab";
        private const string PATH_VFX_SMOKE_BURST = "Assets/VFX/Gadgets/VFX_NullCloud_Burst.prefab";
        private const string PATH_VFX_MINE_LASER = "Assets/VFX/Gadgets/VFX_LogicTrip_Laser.prefab";
        private const string PATH_VFX_MUZZLE = "Assets/VFX/Weapons/VFX_Muzzle_Pistol.prefab";
        private const string PATH_VFX_SPARKS = "Assets/VFX/Systems/VFX_Impact_Sparks.prefab";
        private const string PATH_VFX_MEMORY_DUMP = "Assets/VFX/Systems/VFX_MemoryDump_Reset.prefab";

        // Audio Clips
        private const string PATH_SFX_SYN_FIRE = "Assets/Audio/SFX/Weapons/SFX-WEP-SYN-FIRE.wav";
        private const string PATH_SFX_VEC9_FIRE = "Assets/Audio/SFX/Weapons/SFX-WEP-VEC9-FIRE.wav";
        private const string PATH_SFX_IMPACT = "Assets/Audio/SFX/Impacts/SFX-BULLET-IMPACT-SURFACE.wav";
        private const string PATH_SFX_STEP_WALK = "Assets/Audio/SFX/Locomotion/SFX-PLY-STEP-WALK.wav";
        private const string PATH_SFX_STEP_SPRINT = "Assets/Audio/SFX/Locomotion/SFX-PLY-STEP-SPRINT.wav";
        private const string PATH_SFX_STEP_CROUCH = "Assets/Audio/SFX/Locomotion/SFX-PLY-STEP-CROUCH.wav";
        private const string PATH_SFX_DATACORE_IDLE = "Assets/Audio/SFX/Environment/SFX-ENV-DATACORE-IDLE.wav";
        private const string PATH_SFX_GATEWAY_UNLOCK = "Assets/Audio/SFX/Environment/SFX-ENV-GATEWAY-UNLOCK.wav";
        private const string PATH_SFX_VOID_HUM = "Assets/Audio/Ambience/AMB-VOID-HUM.wav";
        private const string PATH_SFX_COLLAPSE_DRONE = "Assets/Audio/Ambience/AMB-COLLAPSE-DRONE.wav";

        [MenuItem("NullProtocol/Build All Epic 7 Scenes (Level 2 & Level 3)", false, 3)]
        public static void BuildAllEpic7Scenes()
        {
            BuildSubsector02();
            BuildSubsector03RootCore();
            UpdateBuildSettings();
        }

        #region LEVEL 2: SUBSECTOR 02 CORRUPTED ARCHIVES
        [MenuItem("NullProtocol/Build Subsector 02 (Corrupted Archives)", false, 4)]
        public static void BuildSubsector02()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var (playerStateEvents, damageChannel, killChannel, acousticChannel, gadgetChannel, voidResetChannel, radioChannel) = EnsureEventChannels();
            var (synapseArData, vector9Data) = EnsureWeaponData();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            var envRoot = new GameObject("Environment_Subsector02_CorruptedArchives");
            var navSurface = envRoot.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;

            // Deep cyan/purple void lighting
            var sunGo = new GameObject("Void Directional Light");
            sunGo.transform.SetParent(envRoot.transform);
            sunGo.transform.rotation = Quaternion.Euler(60f, -45f, 0f);
            var sunLight = sunGo.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 1.2f;
            sunLight.color = new Color(0.7f, 0.85f, 1.0f);

            // Ambient Void Audio
            var ambGo = new GameObject("Ambience_VoidHum");
            ambGo.transform.SetParent(envRoot.transform);
            var ambAudio = ambGo.AddComponent<AudioSource>();
            ambAudio.loop = true;
            ambAudio.spatialBlend = 0f;
            ambAudio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SFX_VOID_HUM);
            if (ambAudio.clip != null) ambAudio.Play();

            // Room 1: Entry Server Staging Platform (8m x 12m)
            var room01Go = new GameObject("Room_01_ArchiveStaging");
            room01Go.transform.SetParent(envRoot.transform);
            var room01 = room01Go.AddComponent<RoomController>();
            room01.Initialize("Room_01", "Archive Staging Platform");

            var spawnPoint = new GameObject("PlayerSpawnPoint");
            spawnPoint.transform.SetParent(room01Go.transform);
            spawnPoint.transform.position = new Vector3(0f, 0.1f, -4f);
            SetPrivateField(room01, "_spawnPoint", spawnPoint.transform);

            SpawnFloorGrid(room01Go.transform, new Vector2Int(2, 3), new Vector3(0f, 0f, 0f));
            SpawnWall(room01Go.transform, new Vector3(-4f, 0f, -4f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room01Go.transform, new Vector3(4f, 0f, -4f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room01Go.transform, new Vector3(-2f, 0f, -6f), Quaternion.identity);
            SpawnWall(room01Go.transform, new Vector3(2f, 0f, -6f), Quaternion.identity);

            // Void Suspension Pylons underneath
            SpawnVoidPylon(room01Go.transform, new Vector3(-3.5f, -8f, -4f));
            SpawnVoidPylon(room01Go.transform, new Vector3(3.5f, -8f, -4f));

            // Server Rack banks providing tactical cover
            SpawnServerRack(room01Go.transform, new Vector3(-2f, 0f, 2f), Quaternion.identity);
            SpawnServerDebris(room01Go.transform, new Vector3(2.5f, 0f, 3f), Quaternion.Euler(0, -25f, 0));

            // Room 2: Catwalk Network over Endless Void (Z = 6 to 30)
            var room02Go = new GameObject("Room_02_VoidCatwalkAtrium");
            room02Go.transform.SetParent(envRoot.transform);
            var room02 = room02Go.AddComponent<RoomController>();
            room02.Initialize("Room_02", "Void Catwalk Atrium");

            var spawn02 = new GameObject("SpawnPoint_02");
            spawn02.transform.SetParent(room02Go.transform);
            spawn02.transform.position = new Vector3(0f, 0.1f, 7f);
            SetPrivateField(room02, "_spawnPoint", spawn02.transform);

            SpawnThresholdMarker(room02Go.transform, new Vector3(0f, 0f, 6.2f), Quaternion.identity);

            // Narrow suspended catwalk spanning across void
            for (int z = 8; z <= 24; z += 4)
            {
                SpawnCatwalkStraight(room02Go.transform, new Vector3(0f, 0f, z), Quaternion.identity);
                SpawnCatwalkRailing(room02Go.transform, new Vector3(-1f, 0f, z), Quaternion.Euler(0, 90, 0));
                SpawnCatwalkRailing(room02Go.transform, new Vector3(1f, 0f, z), Quaternion.Euler(0, 90, 0));
                SpawnVoidPylon(room02Go.transform, new Vector3(0f, -8f, z));
            }

            // High-Elevation Sniper Catwalk Nest (Y = 4m, Z = 20)
            SpawnCatwalkStairs(room02Go.transform, new Vector3(2f, 0f, 16f), Quaternion.Euler(0, 90, 0));
            SpawnCatwalkStraight(room02Go.transform, new Vector3(6f, 4f, 20f), Quaternion.Euler(0, 90, 0));
            SpawnCatwalkRailing(room02Go.transform, new Vector3(6f, 4f, 22f), Quaternion.identity);
            SpawnCatwalkRailing(room02Go.transform, new Vector3(6f, 4f, 18f), Quaternion.identity);
            SpawnVoidPylon(room02Go.transform, new Vector3(6f, -4f, 20f));

            // Room 3: Corrupted Data Core Platform (Z = 28 to 44)
            var room03Go = new GameObject("Room_03_ArchiveDataCore");
            room03Go.transform.SetParent(envRoot.transform);
            var room03 = room03Go.AddComponent<RoomController>();
            room03.Initialize("Room_03", "Corrupted Data Core Vault");

            var spawn03 = new GameObject("SpawnPoint_03");
            spawn03.transform.SetParent(room03Go.transform);
            spawn03.transform.position = new Vector3(0f, 0.1f, 27f);
            SetPrivateField(room03, "_spawnPoint", spawn03.transform);

            SpawnThresholdMarker(room03Go.transform, new Vector3(0f, 0f, 26.2f), Quaternion.identity);
            SpawnFloorGrid(room03Go.transform, new Vector2Int(3, 4), new Vector3(0f, 0f, 34f));

            // Perimeter walls around Core Vault
            SpawnWall(room03Go.transform, new Vector3(-6f, 0f, 28f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room03Go.transform, new Vector3(-6f, 0f, 32f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room03Go.transform, new Vector3(-6f, 0f, 36f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room03Go.transform, new Vector3(6f, 0f, 28f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room03Go.transform, new Vector3(6f, 0f, 32f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room03Go.transform, new Vector3(6f, 0f, 36f), Quaternion.Euler(0, -90, 0));

            // Data Core Terminal & Cover
            var terminal = SpawnDataCoreTerminal(room03Go.transform, new Vector3(0f, 0f, 34f), Quaternion.identity);
            SpawnServerRack(room03Go.transform, new Vector3(-3.5f, 0f, 34f), Quaternion.Euler(0, 90, 0));
            SpawnServerRack(room03Go.transform, new Vector3(3.5f, 0f, 34f), Quaternion.Euler(0, -90, 0));

            // Extraction Gateway at end of Level 2
            var gateway = SpawnExtractionGateway(room03Go.transform, new Vector3(0f, 0f, 42f), Quaternion.Euler(0, 180, 0));
            gateway.RegisterRequiredTerminal(terminal);
            terminal.TargetGateway = gateway;

            // Player Rig
            var playerRig = BuildPlayerRig(spawnPoint.transform.position, spawnPoint.transform.rotation,
                inputActions, playerStateEvents, damageChannel, killChannel, acousticChannel, gadgetChannel, voidResetChannel,
                synapseArData, vector9Data, out var memDump, out var settingsMgr);

            // Enemies:
            // High sniper sentinel on elevated catwalk (requires smoke to bypass per PRD Story 7.1)
            var sniper = SpawnSentinel(room02Go.transform, "Sentinel_Sniper_HighCatwalk", new Vector3(6f, 4f, 20f), Quaternion.Euler(0, 180, 0), playerRig.transform, acousticChannel, radioChannel, killChannel);
            var sentinelCrossfire = SpawnSentinel(room02Go.transform, "Sentinel_Catwalk_Guard", new Vector3(0f, 0f, 22f), Quaternion.Euler(0, 180, 0), playerRig.transform, acousticChannel, radioChannel, killChannel);

            // Shield Breacher archetype pushing forward in Room 3
            var breacher = SpawnShieldBreacher(room03Go.transform, "ShieldBreacher_ArchiveCore", new Vector3(0f, 0f, 38f), Quaternion.Euler(0, 180, 0), playerRig.transform, acousticChannel, radioChannel, killChannel);
            var flanker = SpawnSentinel(room03Go.transform, "Sentinel_Vault_Flanker", new Vector3(-3.5f, 0f, 36f), Quaternion.Euler(0, 180, 0), playerRig.transform, acousticChannel, radioChannel, killChannel);

            room02.RegisterResettable(sniper);
            room02.RegisterResettable(sentinelCrossfire);
            room03.RegisterResettable(breacher);
            room03.RegisterResettable(flanker);
            room03.RegisterResettable(terminal);
            room03.RegisterResettable(gateway);

            SetPrivateField(memDump, "_activeRoom", room01);

            navSurface.BuildNavMesh();
            EditorSceneManager.SaveScene(scene, SCENE_LEVEL2_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[Epic7SceneBuilder] Subsector 02 (Corrupted Archives) built successfully at " + SCENE_LEVEL2_PATH);
        }
        #endregion

        #region LEVEL 3: ROOT CORE (ARENA COLLAPSE & NULL-01 DUEL)
        [MenuItem("NullProtocol/Build Subsector 03 Root Core (Boss Arena)", false, 5)]
        public static void BuildSubsector03RootCore()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var (playerStateEvents, damageChannel, killChannel, acousticChannel, gadgetChannel, voidResetChannel, radioChannel) = EnsureEventChannels();
            var (synapseArData, vector9Data) = EnsureWeaponData();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            var envRoot = new GameObject("Environment_Subsector03_RootCore");
            var navSurface = envRoot.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;

            // Dramatic Glitch/Void Lighting
            var sunGo = new GameObject("Void Core Light");
            sunGo.transform.SetParent(envRoot.transform);
            sunGo.transform.rotation = Quaternion.Euler(55f, 15f, 0f);
            var sunLight = sunGo.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 2.0f;
            sunLight.color = new Color(1.0f, 0.4f, 0.2f); // Glitch amber/crimson core light

            var ambGo = new GameObject("Ambience_CollapseDrone");
            ambGo.transform.SetParent(envRoot.transform);
            var ambAudio = ambGo.AddComponent<AudioSource>();
            ambAudio.loop = true;
            ambAudio.spatialBlend = 0f;
            ambAudio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SFX_COLLAPSE_DRONE);
            if (ambAudio.clip != null) ambAudio.Play();

            // Root Core Arena (32m x 32m Arena built with Collapsing Hex Tiles)
            var arenaRoomGo = new GameObject("Room_RootCore_Arena");
            arenaRoomGo.transform.SetParent(envRoot.transform);
            var rootRoom = arenaRoomGo.AddComponent<RoomController>();
            rootRoom.Initialize("Room_RootCore", "Root Core - Reality Collapse");

            var spawnPoint = new GameObject("PlayerSpawnPoint");
            spawnPoint.transform.SetParent(arenaRoomGo.transform);
            spawnPoint.transform.position = new Vector3(0f, 0.1f, -12f);
            spawnPoint.transform.rotation = Quaternion.identity;
            SetPrivateField(rootRoom, "_spawnPoint", spawnPoint.transform);

            // Arena Tile Dereferencing Controller (FR-31: 3 waves, 40% reduction)
            var deReferencer = arenaRoomGo.AddComponent<PerimeterTileDeReferencer>();
            SetPrivateField(deReferencer, "_autoTimedWaves", true);
            SetPrivateField(deReferencer, "_timeBeforeFirstWave", 15.0f);
            SetPrivateField(deReferencer, "_waveInterval", 20.0f);
            SetPrivateField(deReferencer, "_warningDuration", 4.0f);

            var allTiles = new List<VoidFallTile>();
            var wave1Tiles = new List<VoidFallTile>();
            var wave2Tiles = new List<VoidFallTile>();
            var wave3Tiles = new List<VoidFallTile>();

            // Generate Hex Arena Floor (radius rings: 0..4)
            // Ring 4: Wave 1 (Outer perimeter drops first)
            // Ring 3: Wave 2 (Middle perimeter drops next)
            // Ring 2: Wave 3 (Inner perimeter drops last, preserving central 60% playable hub)
            // Ring 0-1: Permanent core (Contains Monolith and Boss showdown)
            for (int r = 0; r <= 4; r++)
            {
                int countInRing = (r == 0) ? 1 : r * 6;
                float dist = r * 3.5f;

                for (int i = 0; i < countInRing; i++)
                {
                    float angle = (i * 360f / countInRing) * Mathf.Deg2Rad;
                    Vector3 tilePos = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

                    int assignedWave = 0;
                    if (r == 4) assignedWave = 1;
                    else if (r == 3) assignedWave = 2;
                    else if (r == 2) assignedWave = 3;

                    var tile = SpawnHexTile(arenaRoomGo.transform, tilePos, assignedWave);
                    allTiles.Add(tile);

                    if (assignedWave == 1) wave1Tiles.Add(tile);
                    else if (assignedWave == 2) wave2Tiles.Add(tile);
                    else if (assignedWave == 3) wave3Tiles.Add(tile);
                }
            }

            SetPrivateField(deReferencer, "_allArenaTiles", allTiles);
            SetPrivateField(deReferencer, "_wave1Tiles", wave1Tiles);
            SetPrivateField(deReferencer, "_wave2Tiles", wave2Tiles);
            SetPrivateField(deReferencer, "_wave3Tiles", wave3Tiles);
            rootRoom.RegisterResettable(deReferencer);

            // Center Crystalline Corrupted Monolith (FR-29)
            SpawnCorruptedMonolith(arenaRoomGo.transform, Vector3.zero);

            // Central Data Core Terminal attached to the Monolith
            var terminal = SpawnDataCoreTerminal(arenaRoomGo.transform, new Vector3(0f, 0f, -2.5f), Quaternion.identity);

            // Final Campaign Extraction Gateway
            var gateway = SpawnExtractionGateway(arenaRoomGo.transform, new Vector3(0f, 0f, 6.0f), Quaternion.Euler(0, 180, 0));
            gateway.RegisterRequiredTerminal(terminal);
            terminal.TargetGateway = gateway;
            rootRoom.RegisterResettable(terminal);
            rootRoom.RegisterResettable(gateway);

            // Player Rig
            var playerRig = BuildPlayerRig(spawnPoint.transform.position, spawnPoint.transform.rotation,
                inputActions, playerStateEvents, damageChannel, killChannel, acousticChannel, gadgetChannel, voidResetChannel,
                synapseArData, vector9Data, out var memDump, out var settingsMgr);

            SetPrivateField(memDump, "_activeRoom", rootRoom);

            // Boss AI: Null-01 Mirror Operative (FR-26)
            var boss = SpawnNull01Boss(arenaRoomGo.transform, "Boss_Null01_MirrorOperative", new Vector3(0f, 0f, 10f), Quaternion.Euler(0, 180, 0), playerRig.transform, acousticChannel, radioChannel, killChannel);
            rootRoom.RegisterResettable(boss);

            // 2 Companion Sentinels flanking
            var guardLeft = SpawnSentinel(arenaRoomGo.transform, "Sentinel_BossEscort_Left", new Vector3(-6f, 0f, 6f), Quaternion.Euler(0, 180, 0), playerRig.transform, acousticChannel, radioChannel, killChannel);
            var guardRight = SpawnSentinel(arenaRoomGo.transform, "Sentinel_BossEscort_Right", new Vector3(6f, 0f, 6f), Quaternion.Euler(0, 180, 0), playerRig.transform, acousticChannel, radioChannel, killChannel);
            rootRoom.RegisterResettable(guardLeft);
            rootRoom.RegisterResettable(guardRight);

            navSurface.BuildNavMesh();
            EditorSceneManager.SaveScene(scene, SCENE_LEVEL3_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[Epic7SceneBuilder] Subsector 03 (Root Core) built successfully at " + SCENE_LEVEL3_PATH);
        }
        #endregion

        #region PLAYER RIG & COMMON BUILDERS
        private static GameObject BuildPlayerRig(Vector3 position, Quaternion rotation,
            InputActionAsset inputActions,
            PlayerStateEventChannelSO playerStateEvents,
            DamageEventChannelSO damageChannel,
            KillEventChannelSO killChannel,
            AcousticStimulusEventChannelSO acousticChannel,
            GadgetEventChannelSO gadgetChannel,
            VoidEventChannelSO voidResetChannel,
            WeaponDataSO synapseArData,
            WeaponDataSO vector9Data,
            out MemoryDumpManager memDump,
            out SettingsManager settingsMgr)
        {
            var playerGo = new GameObject("PlayerRig");
            playerGo.tag = Tags.Player;
            playerGo.transform.position = position;
            playerGo.transform.rotation = rotation;

            var charController = playerGo.AddComponent<CharacterController>();
            charController.height = 1.8f;
            charController.radius = 0.4f;
            charController.center = new Vector3(0f, 0.9f, 0f);

            var playerHealth = playerGo.AddComponent<PlayerHealth>();
            SetPrivateField(playerHealth, "_playerStateEvents", playerStateEvents);
            SetPrivateField(playerHealth, "_memoryDumpResetEvents", voidResetChannel);

            var locomotion = playerGo.AddComponent<TacticalLocomotionController>();
            SetPrivateField(locomotion, "_inputActions", inputActions);
            SetPrivateField(locomotion, "_playerStateEvents", playerStateEvents);

            var acousticEmitter = playerGo.AddComponent<LocomotionAcousticEmitter>();
            var playerAudio = playerGo.AddComponent<AudioSource>();
            playerAudio.spatialBlend = 1.0f;
            playerAudio.spatialize = true;
            playerAudio.playOnAwake = false;

            SetPrivateField(acousticEmitter, "_acousticEvents", acousticChannel);
            SetPrivateField(acousticEmitter, "_audioSource", playerAudio);
            SetPrivateField(acousticEmitter, "_walkFootstepClip", AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SFX_STEP_WALK));
            SetPrivateField(acousticEmitter, "_sprintFootstepClip", AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SFX_STEP_SPRINT));
            SetPrivateField(acousticEmitter, "_crouchFootstepClip", AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SFX_STEP_CROUCH));
            SetPrivateField(locomotion, "_acousticEmitter", acousticEmitter);

            var cameraMountGo = new GameObject("CameraMount");
            cameraMountGo.transform.SetParent(playerGo.transform);
            cameraMountGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            cameraMountGo.transform.localRotation = Quaternion.identity;

            var cameraLean = cameraMountGo.AddComponent<CameraLeanController>();
            var cameraLook = cameraMountGo.AddComponent<FirstPersonCameraLook>();
            SetPrivateField(cameraLook, "_playerBody", playerGo.transform);
            SetPrivateField(cameraLook, "_pitchTransform", cameraMountGo.transform);
            SetPrivateField(cameraLook, "_leanController", cameraLean);

            var mainCamGo = new GameObject("Main Camera");
            mainCamGo.tag = Tags.MainCamera;
            mainCamGo.transform.SetParent(cameraMountGo.transform);
            mainCamGo.transform.localPosition = Vector3.zero;
            mainCamGo.transform.localRotation = Quaternion.identity;
            var cam = mainCamGo.AddComponent<Camera>();
            cam.fieldOfView = 90f;
            cam.nearClipPlane = 0.1f;
            mainCamGo.AddComponent<AudioListener>();

            SetPrivateField(locomotion, "_cameraMount", cameraMountGo.transform);
            SetPrivateField(locomotion, "_leanController", cameraLean);
            SetPrivateField(locomotion, "_cameraLook", cameraLook);

            var armsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_FP_ARMS);
            if (armsPrefab != null)
            {
                var armsInstance = Object.Instantiate(armsPrefab, cameraMountGo.transform);
                armsInstance.name = "Viewmodel_Arms";
                armsInstance.transform.localPosition = new Vector3(0f, -0.2f, 0.25f);
                armsInstance.transform.localRotation = Quaternion.identity;
            }

            var watchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_OLED_WATCH);
            GameObject watchInstance = null;
            if (watchPrefab != null)
            {
                watchInstance = Object.Instantiate(watchPrefab, cameraMountGo.transform);
                watchInstance.name = "Forearm_Watch_OLED";
                watchInstance.transform.localPosition = new Vector3(-0.25f, -0.22f, 0.35f);
                watchInstance.transform.localRotation = Quaternion.Euler(15f, 30f, -10f);
            }
            else
            {
                watchInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                watchInstance.name = "Forearm_Watch_Fallback";
                watchInstance.transform.SetParent(cameraMountGo.transform);
                watchInstance.transform.localPosition = new Vector3(-0.25f, -0.22f, 0.35f);
                watchInstance.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
            }

            var watchDisplay = watchInstance.AddComponent<ForearmWatchDisplay>();
            SetPrivateField(watchDisplay, "_oledScreenRenderer", watchInstance.GetComponentInChildren<Renderer>());
            SetPrivateField(watchDisplay, "_playerStateEvents", playerStateEvents);
            SetPrivateField(watchDisplay, "_gadgetEvents", gadgetChannel);

            var weaponMountGo = new GameObject("WeaponMount");
            weaponMountGo.transform.SetParent(mainCamGo.transform);
            weaponMountGo.transform.localPosition = new Vector3(0.2f, -0.2f, 0.4f);
            weaponMountGo.transform.localRotation = Quaternion.identity;

            var swayAndAds = weaponMountGo.AddComponent<WeaponSwayAndADS>();
            SetPrivateField(swayAndAds, "_weaponRootTransform", weaponMountGo.transform);
            SetPrivateField(swayAndAds, "_playerCamera", cam);

            var combatController = playerGo.AddComponent<PlayerCombatController>();
            SetPrivateField(combatController, "_inputActions", inputActions);
            SetPrivateField(combatController, "_cameraMount", cameraMountGo.transform);
            SetPrivateField(combatController, "_playerCamera", cam);
            SetPrivateField(combatController, "_swayAndAds", swayAndAds);

            var economyManager = playerGo.AddComponent<MunitionsEconomyManager>();
            SetPrivateField(economyManager, "_killChannel", killChannel);
            SetPrivateField(economyManager, "_gadgetChannel", gadgetChannel);
            SetPrivateField(combatController, "_economyManager", economyManager);

            var impactSparksPrefab = AssetDatabase.LoadAssetAtPath<ParticleSystem>(PATH_VFX_SPARKS);
            var ricochetClip = AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SFX_IMPACT);

            // Synapse-AR
            var synapseArPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_SYNAPSE_AR);
            var synapseArGo = (synapseArPrefab != null) ? Object.Instantiate(synapseArPrefab, weaponMountGo.transform) : new GameObject("Weapon_SynapseAR");
            synapseArGo.name = "Weapon_SynapseAR";
            synapseArGo.transform.localPosition = Vector3.zero;

            var muzzleGo = new GameObject("MuzzlePoint");
            muzzleGo.transform.SetParent(synapseArGo.transform);
            muzzleGo.transform.localPosition = new Vector3(0f, 0.05f, 0.5f);

            var receiverDisplay = synapseArGo.AddComponent<WeaponReceiverDisplay>();
            SetPrivateField(receiverDisplay, "_receiverRenderer", synapseArGo.GetComponentInChildren<Renderer>());

            var synapseAudio = synapseArGo.AddComponent<AudioSource>();
            synapseAudio.spatialBlend = 1.0f;
            synapseAudio.spatialize = true;

            var synapseVFX = synapseArGo.AddComponent<WeaponVFXController>();
            SetPrivateField(synapseVFX, "_impactSparksPrefab", impactSparksPrefab);
            SetPrivateField(synapseVFX, "_gunshotClip", AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SFX_SYN_FIRE));
            SetPrivateField(synapseVFX, "_ricochetClip", ricochetClip);
            SetPrivateField(synapseVFX, "_audioSource", synapseAudio);

            var synapseWeapon = synapseArGo.AddComponent<SynapseAR>();
            SetPrivateField(synapseWeapon, "_data", synapseArData);
            SetPrivateField(synapseWeapon, "_muzzlePoint", muzzleGo.transform);
            SetPrivateField(synapseWeapon, "_damageChannel", damageChannel);
            SetPrivateField(synapseWeapon, "_killChannel", killChannel);
            SetPrivateField(synapseWeapon, "_acousticChannel", acousticChannel);
            SetPrivateField(synapseWeapon, "_vfx", synapseVFX);

            // Vector-9
            var vector9Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_VECTOR9);
            var vector9Go = (vector9Prefab != null) ? Object.Instantiate(vector9Prefab, weaponMountGo.transform) : new GameObject("Weapon_Vector9");
            vector9Go.name = "Weapon_Vector9";
            vector9Go.transform.localPosition = Vector3.zero;
            vector9Go.SetActive(false);

            var muzzle9 = new GameObject("MuzzlePoint_Vector9");
            muzzle9.transform.SetParent(vector9Go.transform);
            muzzle9.transform.localPosition = new Vector3(0f, 0.04f, 0.28f);

            var vector9Audio = vector9Go.AddComponent<AudioSource>();
            vector9Audio.spatialBlend = 1.0f;
            vector9Audio.spatialize = true;

            var vector9VFX = vector9Go.AddComponent<WeaponVFXController>();
            SetPrivateField(vector9VFX, "_impactSparksPrefab", impactSparksPrefab);
            SetPrivateField(vector9VFX, "_gunshotClip", AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SFX_VEC9_FIRE));
            SetPrivateField(vector9VFX, "_ricochetClip", ricochetClip);
            SetPrivateField(vector9VFX, "_audioSource", vector9Audio);

            var vector9Weapon = vector9Go.AddComponent<Vector9Pistol>();
            SetPrivateField(vector9Weapon, "_data", vector9Data);
            SetPrivateField(vector9Weapon, "_muzzlePoint", muzzle9.transform);
            SetPrivateField(vector9Weapon, "_damageChannel", damageChannel);
            SetPrivateField(vector9Weapon, "_killChannel", killChannel);
            SetPrivateField(vector9Weapon, "_acousticChannel", acousticChannel);
            SetPrivateField(vector9Weapon, "_vfx", vector9VFX);

            var weaponList = new List<BaseWeapon> { synapseWeapon, vector9Weapon };
            SetPrivateField(combatController, "_weapons", weaponList);
            SetPrivateField(combatController, "_currentWeaponIndex", 0);

            var gadgetController = playerGo.AddComponent<PlayerGadgetController>();
            SetPrivateField(gadgetController, "_playerCamera", cam);
            SetPrivateField(gadgetController, "_throwOrigin", muzzleGo.transform);
            SetPrivateField(gadgetController, "_gadgetChannel", gadgetChannel);
            SetPrivateField(gadgetController, "_inputActions", inputActions);
            SetPrivateField(watchDisplay, "_gadgetController", gadgetController);

            SetupGadgetPrefabs(gadgetController);

            var managersRoot = new GameObject("Core_Managers");
            var memDumpGo = new GameObject("MemoryDumpManager");
            memDumpGo.transform.SetParent(managersRoot.transform);
            memDump = memDumpGo.AddComponent<MemoryDumpManager>();
            SetPrivateField(memDump, "_playerStateEvents", playerStateEvents);
            SetPrivateField(memDump, "_memoryDumpResetEvents", voidResetChannel);
            SetPrivateField(memDump, "_playerLocomotion", locomotion);
            SetPrivateField(memDump, "_playerHealth", playerHealth);
            SetPrivateField(memDump, "_playerCombat", combatController);
            SetPrivateField(memDump, "_playerGadgets", gadgetController);

            var memoryDumpVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_VFX_MEMORY_DUMP);
            if (memoryDumpVfxPrefab != null)
            {
                var vfxResetInstance = Object.Instantiate(memoryDumpVfxPrefab, cameraMountGo.transform);
                vfxResetInstance.name = "VFX_MemoryDump_Overlay";
            }

            var settingsGo = new GameObject("SettingsManager");
            settingsGo.transform.SetParent(managersRoot.transform);
            settingsMgr = settingsGo.AddComponent<SettingsManager>();
            SetPrivateField(settingsMgr, "_inputActions", inputActions);
            SetPrivateField(settingsMgr, "_mainCamera", cam);
            SetPrivateField(settingsMgr, "_cameraLook", cameraLook);
            SetPrivateField(settingsMgr, "_weaponSwayAndAds", swayAndAds);
            SetPrivateField(settingsMgr, "_weaponReceiverDisplay", receiverDisplay);
            SetPrivateField(settingsMgr, "_forearmWatchDisplay", watchDisplay);

            return playerGo;
        }

        private static void SetupGadgetPrefabs(PlayerGadgetController gadgetController)
        {
            var puckGo = new GameObject("Prefab_BarricadePuck");
            puckGo.SetActive(false);
            var puckModel = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_GAD_PUCK);
            if (puckModel != null) Object.Instantiate(puckModel, puckGo.transform);
            var puckSphere = puckGo.AddComponent<SphereCollider>();
            puckSphere.radius = 0.2f;
            var puckComp = puckGo.AddComponent<BarricadePuck>();
            SetPrivateField(gadgetController, "_puckPrefab", puckComp);

            var barricadeGo = new GameObject("Prefab_DeployableBarricade");
            barricadeGo.SetActive(false);
            var barrierModel = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_GAD_BARRIER);
            if (barrierModel != null) Object.Instantiate(barrierModel, barricadeGo.transform);
            var barricadeComp = barricadeGo.AddComponent<DeployableBarricade>();
            SetPrivateField(gadgetController, "_barricadePrefab", barricadeComp);

            var canisterGo = new GameObject("Prefab_SmokeCanister");
            canisterGo.SetActive(false);
            var canisterModel = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_GAD_CANISTER);
            if (canisterModel != null) Object.Instantiate(canisterModel, canisterGo.transform);
            var canisterSphere = canisterGo.AddComponent<SphereCollider>();
            canisterSphere.radius = 0.15f;
            var canisterComp = canisterGo.AddComponent<SmokeCanister>();
            SetPrivateField(gadgetController, "_smokeCanisterPrefab", canisterComp);

            var smokeGo = new GameObject("Prefab_NullCloudSmoke");
            smokeGo.SetActive(false);
            var vfxSmoke = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_VFX_SMOKE_BURST);
            if (vfxSmoke != null) Object.Instantiate(vfxSmoke, smokeGo.transform);
            var smokeSphere = smokeGo.AddComponent<SphereCollider>();
            smokeSphere.isTrigger = true;
            smokeSphere.radius = 2.0f;
            var smokeComp = smokeGo.AddComponent<NullCloudSmoke>();
            SetPrivateField(gadgetController, "_smokePrefab", smokeComp);

            var mineGo = new GameObject("Prefab_LogicTripMine");
            mineGo.SetActive(false);
            var mineModel = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_GAD_MINE);
            if (mineModel != null) Object.Instantiate(mineModel, mineGo.transform);
            var mineBox = mineGo.AddComponent<BoxCollider>();
            mineBox.size = new Vector3(0.2f, 0.2f, 0.2f);
            var mineComp = mineGo.AddComponent<LogicTripMine>();
            SetPrivateField(gadgetController, "_tripMinePrefab", mineComp);
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true),
                new EditorBuildSettingsScene(SCENE_LEVEL2_PATH, true),
                new EditorBuildSettingsScene(SCENE_LEVEL3_PATH, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Epic7SceneBuilder] Updated EditorBuildSettings with all 3 campaign levels!");
        }
        #endregion

        #region ENVIRONMENT HELPER SPAWNERS
        private static void SpawnFloorGrid(Transform parent, Vector2Int count, Vector3 center)
        {
            var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_FLOOR);
            float startX = center.x - (count.x - 1) * 2.0f;
            float startZ = center.z - (count.y - 1) * 2.0f;

            for (int x = 0; x < count.x; x++)
            {
                for (int z = 0; z < count.y; z++)
                {
                    Vector3 pos = new Vector3(startX + x * 4.0f, center.y, startZ + z * 4.0f);
                    var floor = (floorPrefab != null) ? Object.Instantiate(floorPrefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                    floor.transform.position = pos;
                    floor.transform.rotation = Quaternion.identity;
                    EnsureBoxCollider(floor, new Vector3(4f, 0.2f, 4f), new Vector3(0f, -0.1f, 0f));
                }
            }
        }

        private static GameObject SpawnWall(Transform parent, Vector3 pos, Quaternion rot)
        {
            var wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_WALL);
            var wall = (wallPrefab != null) ? Object.Instantiate(wallPrefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = pos;
            wall.transform.rotation = rot;
            wall.tag = Tags.GridWall;
            EnsureBoxCollider(wall, new Vector3(4f, 4f, 0.4f), new Vector3(0f, 2f, 0f));
            return wall;
        }

        private static void SpawnCatwalkStraight(Transform parent, Vector3 pos, Quaternion rot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_CATWALK_STRAIGHT);
            var go = (prefab != null) ? Object.Instantiate(prefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.rotation = rot;
            EnsureBoxCollider(go, new Vector3(2.0f, 0.3f, 4.0f), new Vector3(0f, 0.15f, 0f));
        }

        private static void SpawnCatwalkStairs(Transform parent, Vector3 pos, Quaternion rot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_CATWALK_STAIRS);
            var go = (prefab != null) ? Object.Instantiate(prefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.rotation = rot;
            EnsureBoxCollider(go, new Vector3(4.0f, 4.0f, 4.0f), new Vector3(0f, 2.0f, 0f));
        }

        private static void SpawnCatwalkRailing(Transform parent, Vector3 pos, Quaternion rot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_CATWALK_RAILING);
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab, parent);
                go.transform.position = pos;
                go.transform.rotation = rot;
                EnsureBoxCollider(go, new Vector3(0.1f, 1.1f, 4.0f), new Vector3(0f, 0.55f, 0f));
            }
        }

        private static void SpawnVoidPylon(Transform parent, Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_VOID_PYLON);
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab, parent);
                go.transform.position = pos;
            }
        }

        private static void SpawnServerRack(Transform parent, Vector3 pos, Quaternion rot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_SERVER_RACK);
            var go = (prefab != null) ? Object.Instantiate(prefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.tag = Tags.GridWall;
            EnsureBoxCollider(go, new Vector3(3.8f, 4.0f, 1.2f), new Vector3(0f, 2.0f, 0f));
        }

        private static void SpawnServerDebris(Transform parent, Vector3 pos, Quaternion rot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_SERVER_DEBRIS);
            var go = (prefab != null) ? Object.Instantiate(prefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.tag = Tags.GridWall;
            EnsureBoxCollider(go, new Vector3(1.5f, 1.0f, 1.0f), new Vector3(0f, 0.5f, 0f));
        }

        private static VoidFallTile SpawnHexTile(Transform parent, Vector3 pos, int waveIndex)
        {
            var hexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_HEX_TILE);
            var go = (hexPrefab != null) ? Object.Instantiate(hexPrefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;

            var col = EnsureBoxCollider(go, new Vector3(3.8f, 0.4f, 3.8f), new Vector3(0f, -0.2f, 0f));
            var tile = go.AddComponent<VoidFallTile>();
            tile.WaveIndex = waveIndex;
            return tile;
        }

        private static void SpawnCorruptedMonolith(Transform parent, Vector3 pos)
        {
            var monolithPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_MONOLITH);
            var go = (monolithPrefab != null) ? Object.Instantiate(monolithPrefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.tag = Tags.GridWall;
            EnsureBoxCollider(go, new Vector3(2.0f, 6.0f, 2.0f), new Vector3(0f, 3.0f, 0f));
        }

        private static void SpawnThresholdMarker(Transform parent, Vector3 pos, Quaternion rot)
        {
            var markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_THRESHOLD);
            if (markerPrefab != null)
            {
                var marker = Object.Instantiate(markerPrefab, parent);
                marker.transform.position = pos;
                marker.transform.rotation = rot;
            }
        }

        private static DataCoreTerminal SpawnDataCoreTerminal(Transform parent, Vector3 pos, Quaternion rot)
        {
            var terminalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_TERMINAL);
            var go = (terminalPrefab != null) ? Object.Instantiate(terminalPrefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.position = pos;
            go.transform.rotation = rot;
            EnsureBoxCollider(go, new Vector3(1.2f, 1.6f, 0.8f), new Vector3(0f, 0.8f, 0f));

            var audio = go.AddComponent<AudioSource>();
            audio.spatialBlend = 1.0f;
            audio.loop = true;
            audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SFX_DATACORE_IDLE);
            if (audio.clip != null) audio.Play();

            return go.AddComponent<DataCoreTerminal>();
        }

        private static ExtractionGateway SpawnExtractionGateway(Transform parent, Vector3 pos, Quaternion rot)
        {
            var gatewayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_GATEWAY);
            var go = (gatewayPrefab != null) ? Object.Instantiate(gatewayPrefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.rotation = rot;
            var col = EnsureBoxCollider(go, new Vector3(4f, 4f, 0.6f), new Vector3(0f, 2f, 0f));

            var audio = go.AddComponent<AudioSource>();
            audio.spatialBlend = 1.0f;
            audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(PATH_SFX_GATEWAY_UNLOCK);

            var gateway = go.AddComponent<ExtractionGateway>();
            SetPrivateField(gateway, "_physicalBarrier", col);
            return gateway;
        }

        private static StandardSentinel SpawnSentinel(Transform parent, string name, Vector3 pos, Quaternion rot, Transform playerTransform, AcousticStimulusEventChannelSO acousticChannel, AIRadioChatterEventChannelSO radioChannel, KillEventChannelSO killChannel)
        {
            var sentinelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_SENTINEL);
            var go = (sentinelPrefab != null) ? Object.Instantiate(sentinelPrefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.tag = Tags.Enemy;

            var bodyCol = go.GetComponent<CapsuleCollider>();
            if (bodyCol == null) bodyCol = go.AddComponent<CapsuleCollider>();
            bodyCol.height = 1.8f;
            bodyCol.radius = 0.4f;
            bodyCol.center = new Vector3(0f, 0.9f, 0f);

            var health = go.AddComponent<SentinelHealth>();
            SetPrivateField(health, "_killChannel", killChannel);

            var headGo = new GameObject("Sentinel_Head");
            headGo.tag = Tags.EnemyHead;
            headGo.transform.SetParent(go.transform);
            headGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var headCol = headGo.AddComponent<SphereCollider>();
            headCol.radius = 0.22f;

            var navAgent = go.GetComponent<NavMeshAgent>();
            if (navAgent == null) navAgent = go.AddComponent<NavMeshAgent>();
            navAgent.speed = 2.5f;

            var perception = go.AddComponent<SentinelPerception>();
            SetPrivateField(perception, "_eyeTransform", headGo.transform);
            SetPrivateField(perception, "_acousticChannel", acousticChannel);

            var radio = go.AddComponent<SentinelRadioChatter>();
            SetPrivateField(radio, "_radioChannel", radioChannel);

            var standardSentinel = go.AddComponent<StandardSentinel>();
            SetPrivateField(standardSentinel, "_perception", perception);
            SetPrivateField(standardSentinel, "_radioChatter", radio);
            SetPrivateField(standardSentinel, "_health", health);
            SetPrivateField(standardSentinel, "_navAgent", navAgent);
            standardSentinel.SetTargetPlayer(playerTransform);

            return standardSentinel;
        }

        private static ShieldBreacher SpawnShieldBreacher(Transform parent, string name, Vector3 pos, Quaternion rot, Transform playerTransform, AcousticStimulusEventChannelSO acousticChannel, AIRadioChatterEventChannelSO radioChannel, KillEventChannelSO killChannel)
        {
            var breacherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_BREACHER);
            var go = (breacherPrefab != null) ? Object.Instantiate(breacherPrefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.tag = Tags.Enemy;

            var bodyCol = go.GetComponent<CapsuleCollider>();
            if (bodyCol == null) bodyCol = go.AddComponent<CapsuleCollider>();
            bodyCol.height = 1.8f;
            bodyCol.radius = 0.45f;
            bodyCol.center = new Vector3(0f, 0.9f, 0f);

            // Shield attachment
            var shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_BREACHER_SHIELD);
            if (shieldPrefab != null)
            {
                var shieldInstance = Object.Instantiate(shieldPrefab, go.transform);
                shieldInstance.name = "Frontal_HardLight_Shield";
                shieldInstance.transform.localPosition = new Vector3(0f, 0.9f, 0.5f);
            }

            var navAgent = go.GetComponent<NavMeshAgent>();
            if (navAgent == null) navAgent = go.AddComponent<NavMeshAgent>();
            navAgent.speed = 1.6f;

            var perception = go.AddComponent<SentinelPerception>();
            var eyeGo = new GameObject("Breacher_Eyes");
            eyeGo.transform.SetParent(go.transform);
            eyeGo.transform.localPosition = new Vector3(0f, 1.65f, 0.2f);
            SetPrivateField(perception, "_eyeTransform", eyeGo.transform);
            SetPrivateField(perception, "_acousticChannel", acousticChannel);

            var radio = go.AddComponent<SentinelRadioChatter>();
            SetPrivateField(radio, "_radioChannel", radioChannel);

            var breacher = go.AddComponent<ShieldBreacher>();
            SetPrivateField(breacher, "_perception", perception);
            SetPrivateField(breacher, "_radioChatter", radio);
            SetPrivateField(breacher, "_killChannel", killChannel);
            SetPrivateField(breacher, "_navAgent", navAgent);
            breacher.SetTargetPlayer(playerTransform);

            return breacher;
        }

        private static Null01Boss SpawnNull01Boss(Transform parent, string name, Vector3 pos, Quaternion rot, Transform playerTransform, AcousticStimulusEventChannelSO acousticChannel, AIRadioChatterEventChannelSO radioChannel, KillEventChannelSO killChannel)
        {
            var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_NULL01_BOSS);
            var go = (bossPrefab != null) ? Object.Instantiate(bossPrefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.tag = Tags.Enemy;

            var bodyCol = go.GetComponent<CapsuleCollider>();
            if (bodyCol == null) bodyCol = go.AddComponent<CapsuleCollider>();
            bodyCol.height = 1.8f;
            bodyCol.radius = 0.4f;
            bodyCol.center = new Vector3(0f, 0.9f, 0f);

            var headGo = new GameObject("Boss_Head");
            headGo.tag = Tags.EnemyHead;
            headGo.transform.SetParent(go.transform);
            headGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var headCol = headGo.AddComponent<SphereCollider>();
            headCol.radius = 0.25f;

            var navAgent = go.GetComponent<NavMeshAgent>();
            if (navAgent == null) navAgent = go.AddComponent<NavMeshAgent>();
            navAgent.speed = 3.5f;

            var perception = go.AddComponent<SentinelPerception>();
            SetPrivateField(perception, "_eyeTransform", headGo.transform);
            SetPrivateField(perception, "_acousticChannel", acousticChannel);

            var radio = go.AddComponent<SentinelRadioChatter>();
            SetPrivateField(radio, "_radioChannel", radioChannel);

            var boss = go.AddComponent<Null01Boss>();
            SetPrivateField(boss, "_perception", perception);
            SetPrivateField(boss, "_radioChatter", radio);
            SetPrivateField(boss, "_killChannel", killChannel);
            SetPrivateField(boss, "_navAgent", navAgent);
            boss.SetTargetPlayer(playerTransform);

            return boss;
        }

        private static BoxCollider EnsureBoxCollider(GameObject obj, Vector3 size, Vector3 center)
        {
            var col = obj.GetComponent<BoxCollider>();
            if (col == null) col = obj.AddComponent<BoxCollider>();
            col.size = size;
            col.center = center;
            return col;
        }
        #endregion

        #region ScriptableObjects & Reflection
        private static void EnsureDirectories()
        {
            if (!Directory.Exists(EVENT_CHANNELS_FOLDER)) Directory.CreateDirectory(EVENT_CHANNELS_FOLDER);
            if (!Directory.Exists(WEAPON_DATA_FOLDER)) Directory.CreateDirectory(WEAPON_DATA_FOLDER);
        }

        private static (PlayerStateEventChannelSO, DamageEventChannelSO, KillEventChannelSO, AcousticStimulusEventChannelSO, GadgetEventChannelSO, VoidEventChannelSO, AIRadioChatterEventChannelSO) EnsureEventChannels()
        {
            var playerState = LoadOrCreateSO<PlayerStateEventChannelSO>($"{EVENT_CHANNELS_FOLDER}/PlayerStateEventChannel.asset");
            var damage = LoadOrCreateSO<DamageEventChannelSO>($"{EVENT_CHANNELS_FOLDER}/DamageEventChannel.asset");
            var kill = LoadOrCreateSO<KillEventChannelSO>($"{EVENT_CHANNELS_FOLDER}/KillEventChannel.asset");
            var acoustic = LoadOrCreateSO<AcousticStimulusEventChannelSO>($"{EVENT_CHANNELS_FOLDER}/AcousticStimulusEventChannel.asset");
            var gadget = LoadOrCreateSO<GadgetEventChannelSO>($"{EVENT_CHANNELS_FOLDER}/GadgetEventChannel.asset");
            var voidReset = LoadOrCreateSO<VoidEventChannelSO>($"{EVENT_CHANNELS_FOLDER}/MemoryDumpResetEventChannel.asset");
            var radio = LoadOrCreateSO<AIRadioChatterEventChannelSO>($"{EVENT_CHANNELS_FOLDER}/AIRadioChatterEventChannel.asset");

            return (playerState, damage, kill, acoustic, gadget, voidReset, radio);
        }

        private static (WeaponDataSO, WeaponDataSO) EnsureWeaponData()
        {
            var synapse = LoadOrCreateSO<WeaponDataSO>($"{WEAPON_DATA_FOLDER}/SynapseAR_Data.asset");
            var vector9 = LoadOrCreateSO<WeaponDataSO>($"{WEAPON_DATA_FOLDER}/Vector9_Data.asset");
            return (synapse, vector9);
        }

        private static T LoadOrCreateSO<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                type = type.BaseType;
            }

            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[Epic7SceneBuilder] Field '{fieldName}' not found on '{target.GetType().Name}'.");
            }
        }
        #endregion
    }
}
