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
    /// Production Level Builder for Subsector 01 (Tactical Orientation / Brutalist Corridors).
    /// Assembles hand-crafted 4m grid rooms with real production 3D models, diegetic UI,
    /// Hitscan weapon sockets, Sentinel AI archetypes, and Memory-Dump Loop (FR-28 to FR-33, EP-06).
    /// </summary>
    public static class ProductionSceneBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";
        private const string EVENT_CHANNELS_FOLDER = "Assets/_Project/ScriptableObjects/Events";
        private const string WEAPON_DATA_FOLDER = "Assets/_Project/ScriptableObjects/Weapons";

        // Asset Paths for 3D Models
        private const string PATH_FLOOR = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_01_FloorTile.glb";
        private const string PATH_WALL = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_02_SolidWall.glb";
        private const string PATH_DOORFRAME = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_03_WallDoorframe.glb";
        private const string PATH_CORNER = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_04_CornerColumn.glb";
        private const string PATH_COVER_PILLAR = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_05_CoverPillar.glb";
        private const string PATH_LOW_COVER = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_06_LowCoverHalfWall.glb";
        private const string PATH_THRESHOLD = "Assets/Art/Environment/ModularKit_4m/ENV_MOD_15_ThresholdMarker.glb";
        private const string PATH_TERMINAL = "Assets/Art/Environment/Props/ENV_PROP_01_DataCoreTerminal.glb";
        private const string PATH_GATEWAY = "Assets/Art/Environment/Props/ENV_PROP_02_ExtractionGateway.glb";
        private const string PATH_FP_ARMS = "Assets/Art/Characters/FirstPersonViewmodel/CHR_FP_ARMS.glb";
        private const string PATH_OLED_WATCH = "Assets/Art/Characters/CHR_OLED_WATCH.glb";
        private const string PATH_SYNAPSE_AR = "Assets/Art/Weapons/SynapseAR/WEP_MOD_02_SynapseAR.glb";
        private const string PATH_VECTOR9 = "Assets/Art/Weapons/Vector9/WEP_MOD_01_Vector9.glb";
        private const string PATH_SENTINEL = "Assets/Art/Characters/Enemies/Sentinel/CHR_AI_SENTINEL.glb";

        [MenuItem("NullProtocol/Build Production Subsector 01 Scene", false, 2)]
        public static void BuildProductionSubsector01()
        {
            EnsureDirectories();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            if (inputActions == null)
            {
                Debug.LogError($"[ProductionSceneBuilder] Missing InputActionAsset at '{INPUT_ACTIONS_PATH}'.");
                return;
            }

            var (playerStateEvents, damageChannel, killChannel, acousticChannel, gadgetChannel, voidResetChannel, radioChannel) = EnsureEventChannels();
            var (synapseArData, vector9Data) = EnsureWeaponData();

            // Open or initialize scene
            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            var rootObjects = scene.GetRootGameObjects();
            if (rootObjects.Length > 0)
            {
                Undo.RegisterFullObjectHierarchyUndo(rootObjects[0], "Build Production Subsector 01");
            }

            for (int i = rootObjects.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(rootObjects[i]);
            }

            // 1. Atmosphere & Lighting Root
            var envRoot = new GameObject("Environment_ModularSubsector01");
            var navSurface = envRoot.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;

            var sunGo = new GameObject("Directional Light");
            sunGo.transform.SetParent(envRoot.transform);
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.8f;
            light.color = new Color(0.95f, 0.95f, 1.0f);

            // 2. Room 1: Subsector Alpha Entry & Infiltration Corridor (8m x 12m)
            var room01Go = new GameObject("Room_01_AlphaEntry");
            room01Go.transform.SetParent(envRoot.transform);
            var room01 = room01Go.AddComponent<RoomController>();
            room01.Initialize("Room_01", "Subsector Alpha Entry");

            var spawnPointGo = new GameObject("PlayerSpawnPoint");
            spawnPointGo.transform.SetParent(room01Go.transform);
            spawnPointGo.transform.position = new Vector3(0f, 0.1f, -4f);
            spawnPointGo.transform.rotation = Quaternion.identity;
            SetPrivateField(room01, "_spawnPoint", spawnPointGo.transform);

            // Floor 2x3 (X: -2, 2; Z: -4, 0, 4)
            SpawnFloorGrid(room01Go.transform, new Vector2Int(2, 3), new Vector3(0f, 0f, 0f));

            // Perimeter walls
            SpawnWall(room01Go.transform, new Vector3(-4f, 0f, -4f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room01Go.transform, new Vector3(-4f, 0f, 0f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room01Go.transform, new Vector3(-4f, 0f, 4f), Quaternion.Euler(0, 90, 0));

            SpawnWall(room01Go.transform, new Vector3(4f, 0f, -4f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room01Go.transform, new Vector3(4f, 0f, 0f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room01Go.transform, new Vector3(4f, 0f, 4f), Quaternion.Euler(0, -90, 0));

            SpawnWall(room01Go.transform, new Vector3(-2f, 0f, -6f), Quaternion.identity);
            SpawnWall(room01Go.transform, new Vector3(2f, 0f, -6f), Quaternion.identity);

            // Chokepoint partition at Z = 6 with 4m Doorframe in center
            SpawnWall(room01Go.transform, new Vector3(-3f, 0f, 6f), Quaternion.identity);
            SpawnDoorframe(room01Go.transform, new Vector3(0f, 0f, 6f), Quaternion.identity);
            SpawnWall(room01Go.transform, new Vector3(3f, 0f, 6f), Quaternion.identity);

            // Tactical cover: 1 Cover Pillar to break sightline (FR-28: <= 12m sightlines)
            SpawnCoverPillar(room01Go.transform, new Vector3(-1.8f, 0f, 2f), CoverHeight.High, Vector3.forward);

            // 3. Room 2: Server Breach Hub (12m x 16m) (Z from 6 to 22)
            var room02Go = new GameObject("Room_02_ServerBreachHub");
            room02Go.transform.SetParent(envRoot.transform);
            var room02 = room02Go.AddComponent<RoomController>();
            room02.Initialize("Room_02", "Server Breach Hub");

            var spawnPoint02 = new GameObject("SpawnPoint_02");
            spawnPoint02.transform.SetParent(room02Go.transform);
            spawnPoint02.transform.position = new Vector3(0f, 0.1f, 7f);
            SetPrivateField(room02, "_spawnPoint", spawnPoint02.transform);

            // Threshold marker at doorway entrance (Z = 6.2)
            SpawnThresholdMarker(room02Go.transform, new Vector3(0f, 0f, 6.2f), Quaternion.identity);

            // Floor 3x4 (X: -4, 0, 4; Z: 8, 12, 16, 20)
            SpawnFloorGrid(room02Go.transform, new Vector2Int(3, 4), new Vector3(0f, 0f, 14f));

            // Room 2 Perimeter Walls
            SpawnWall(room02Go.transform, new Vector3(-6f, 0f, 8f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room02Go.transform, new Vector3(-6f, 0f, 12f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room02Go.transform, new Vector3(-6f, 0f, 16f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room02Go.transform, new Vector3(-6f, 0f, 20f), Quaternion.Euler(0, 90, 0));

            SpawnWall(room02Go.transform, new Vector3(6f, 0f, 8f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room02Go.transform, new Vector3(6f, 0f, 12f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room02Go.transform, new Vector3(6f, 0f, 16f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room02Go.transform, new Vector3(6f, 0f, 20f), Quaternion.Euler(0, -90, 0));

            // Low tactical cover for active crouching / peeking (FR-2, FR-6)
            SpawnLowCover(room02Go.transform, new Vector3(-2f, 0f, 12f), Quaternion.identity, CoverHeight.Low, Vector3.forward);
            SpawnLowCover(room02Go.transform, new Vector3(2.5f, 0f, 16f), Quaternion.Euler(0, 180, 0), CoverHeight.Low, -Vector3.forward);
            SpawnCoverPillar(room02Go.transform, new Vector3(3f, 0f, 11f), CoverHeight.High, -Vector3.right);

            // Doorframe connecting Room 2 to Room 3 at Z = 22
            SpawnWall(room02Go.transform, new Vector3(-4f, 0f, 22f), Quaternion.identity);
            SpawnDoorframe(room02Go.transform, new Vector3(0f, 0f, 22f), Quaternion.identity);
            SpawnWall(room02Go.transform, new Vector3(4f, 0f, 22f), Quaternion.identity);

            // 4. Room 3: Data Core Chamber (12m x 16m) (Z from 22 to 38)
            var room03Go = new GameObject("Room_03_DataCoreChamber");
            room03Go.transform.SetParent(envRoot.transform);
            var room03 = room03Go.AddComponent<RoomController>();
            room03.Initialize("Room_03", "Data Core Chamber");

            var spawnPoint03 = new GameObject("SpawnPoint_03");
            spawnPoint03.transform.SetParent(room03Go.transform);
            spawnPoint03.transform.position = new Vector3(0f, 0.1f, 23f);
            SetPrivateField(room03, "_spawnPoint", spawnPoint03.transform);

            SpawnThresholdMarker(room03Go.transform, new Vector3(0f, 0f, 22.2f), Quaternion.identity);

            // Floor 3x4 (X: -4, 0, 4; Z: 24, 28, 32, 36)
            SpawnFloorGrid(room03Go.transform, new Vector2Int(3, 4), new Vector3(0f, 0f, 30f));

            // Room 3 Perimeter Walls
            SpawnWall(room03Go.transform, new Vector3(-6f, 0f, 24f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room03Go.transform, new Vector3(-6f, 0f, 28f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room03Go.transform, new Vector3(-6f, 0f, 32f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room03Go.transform, new Vector3(-6f, 0f, 36f), Quaternion.Euler(0, 90, 0));

            SpawnWall(room03Go.transform, new Vector3(6f, 0f, 24f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room03Go.transform, new Vector3(6f, 0f, 28f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room03Go.transform, new Vector3(6f, 0f, 32f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room03Go.transform, new Vector3(6f, 0f, 36f), Quaternion.Euler(0, -90, 0));

            // Central Data Core Terminal (FR-32: Mission Objective)
            var terminal = SpawnDataCoreTerminal(room03Go.transform, new Vector3(0f, 0f, 30f), Quaternion.identity);

            // Defensive pillars flanking the Data Core
            SpawnCoverPillar(room03Go.transform, new Vector3(-3.5f, 0f, 30f), CoverHeight.High, Vector3.right);
            SpawnCoverPillar(room03Go.transform, new Vector3(3.5f, 0f, 30f), CoverHeight.High, -Vector3.right);
            SpawnLowCover(room03Go.transform, new Vector3(0f, 0f, 33f), Quaternion.Euler(0, 180, 0), CoverHeight.Low, Vector3.forward);

            // Partition at Z = 38 leading to Extraction Corridor
            SpawnWall(room03Go.transform, new Vector3(-3f, 0f, 38f), Quaternion.identity);
            SpawnDoorframe(room03Go.transform, new Vector3(0f, 0f, 38f), Quaternion.identity);
            SpawnWall(room03Go.transform, new Vector3(3f, 0f, 38f), Quaternion.identity);

            // 5. Room 4: Extraction Corridor & Security Gateway (8m x 12m) (Z from 38 to 50)
            var room04Go = new GameObject("Room_04_ExtractionSecurity");
            room04Go.transform.SetParent(envRoot.transform);
            var room04 = room04Go.AddComponent<RoomController>();
            room04.Initialize("Room_04", "Extraction Gateway Corridor");

            var spawnPoint04 = new GameObject("SpawnPoint_04");
            spawnPoint04.transform.SetParent(room04Go.transform);
            spawnPoint04.transform.position = new Vector3(0f, 0.1f, 39f);
            SetPrivateField(room04, "_spawnPoint", spawnPoint04.transform);

            SpawnThresholdMarker(room04Go.transform, new Vector3(0f, 0f, 38.2f), Quaternion.identity);

            // Floor 2x3 (X: -2, 2; Z: 40, 44, 48)
            SpawnFloorGrid(room04Go.transform, new Vector2Int(2, 3), new Vector3(0f, 0f, 44f));

            SpawnWall(room04Go.transform, new Vector3(-4f, 0f, 40f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room04Go.transform, new Vector3(-4f, 0f, 44f), Quaternion.Euler(0, 90, 0));
            SpawnWall(room04Go.transform, new Vector3(-4f, 0f, 48f), Quaternion.Euler(0, 90, 0));

            SpawnWall(room04Go.transform, new Vector3(4f, 0f, 40f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room04Go.transform, new Vector3(4f, 0f, 44f), Quaternion.Euler(0, -90, 0));
            SpawnWall(room04Go.transform, new Vector3(4f, 0f, 48f), Quaternion.Euler(0, -90, 0));

            // Extraction Gateway at end of corridor (Z = 50)
            var gateway = SpawnExtractionGateway(room04Go.transform, new Vector3(0f, 0f, 50f), Quaternion.Euler(0, 180, 0));
            gateway.RegisterRequiredTerminal(terminal);
            terminal.TargetGateway = gateway;

            // 6. Assembled First-Person Player Rig
            var playerGo = new GameObject("PlayerRig");
            playerGo.tag = Tags.Player;
            playerGo.transform.position = spawnPointGo.transform.position;
            playerGo.transform.rotation = spawnPointGo.transform.rotation;

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
            SetPrivateField(acousticEmitter, "_acousticChannel", acousticChannel);
            SetPrivateField(locomotion, "_acousticEmitter", acousticEmitter);

            // Camera Mount & Leaning (FR-5: 18 deg roll, 0.35m lateral displacement)
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

            // First Person Viewmodel Arms (CHR_FP_ARMS.glb)
            var armsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_FP_ARMS);
            GameObject armsInstance = null;
            if (armsPrefab != null)
            {
                armsInstance = Object.Instantiate(armsPrefab, cameraMountGo.transform);
                armsInstance.name = "Viewmodel_Arms";
                armsInstance.transform.localPosition = new Vector3(0f, -0.2f, 0.25f);
                armsInstance.transform.localRotation = Quaternion.identity;
            }

            // Forearm Watch Display (CHR_OLED_WATCH.glb)
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
                var c = watchInstance.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }

            var watchDisplay = watchInstance.AddComponent<ForearmWatchDisplay>();
            SetPrivateField(watchDisplay, "_oledScreenRenderer", watchInstance.GetComponentInChildren<Renderer>());
            SetPrivateField(watchDisplay, "_playerStateEvents", playerStateEvents);
            SetPrivateField(watchDisplay, "_gadgetEvents", gadgetChannel);

            // Weapon Mount & Sway / ADS
            var weaponMountGo = new GameObject("WeaponMount");
            weaponMountGo.transform.SetParent(mainCamGo.transform);
            weaponMountGo.transform.localPosition = new Vector3(0.2f, -0.2f, 0.4f);
            weaponMountGo.transform.localRotation = Quaternion.identity;

            var swayAndAds = weaponMountGo.AddComponent<WeaponSwayAndADS>();
            SetPrivateField(swayAndAds, "_weaponMount", weaponMountGo.transform);
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

            // Primary Weapon: Synapse-AR (WEP_MOD_02_SynapseAR.glb)
            var synapseArPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_SYNAPSE_AR);
            GameObject synapseArGo;
            if (synapseArPrefab != null)
            {
                synapseArGo = Object.Instantiate(synapseArPrefab, weaponMountGo.transform);
                synapseArGo.name = "Weapon_SynapseAR";
                synapseArGo.transform.localPosition = Vector3.zero;
                synapseArGo.transform.localRotation = Quaternion.identity;
            }
            else
            {
                synapseArGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                synapseArGo.name = "Weapon_SynapseAR_Fallback";
                synapseArGo.transform.SetParent(weaponMountGo.transform);
                synapseArGo.transform.localPosition = Vector3.zero;
                synapseArGo.transform.localScale = new Vector3(0.08f, 0.12f, 0.5f);
                var c = synapseArGo.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }

            var muzzleGo = new GameObject("MuzzlePoint");
            muzzleGo.transform.SetParent(synapseArGo.transform);
            muzzleGo.transform.localPosition = new Vector3(0f, 0.05f, 0.5f);

            var receiverDisplay = synapseArGo.AddComponent<WeaponReceiverDisplay>();
            SetPrivateField(receiverDisplay, "_receiverRenderer", synapseArGo.GetComponentInChildren<Renderer>());

            var synapseWeapon = synapseArGo.AddComponent<SynapseAR>();
            SetPrivateField(synapseWeapon, "_data", synapseArData);
            SetPrivateField(synapseWeapon, "_muzzlePoint", muzzleGo.transform);
            SetPrivateField(synapseWeapon, "_damageChannel", damageChannel);
            SetPrivateField(synapseWeapon, "_killChannel", killChannel);
            SetPrivateField(synapseWeapon, "_acousticChannel", acousticChannel);

            // Secondary Weapon: Vector-9 (WEP_MOD_01_Vector9.glb)
            var vector9Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_VECTOR9);
            GameObject vector9Go;
            if (vector9Prefab != null)
            {
                vector9Go = Object.Instantiate(vector9Prefab, weaponMountGo.transform);
                vector9Go.name = "Weapon_Vector9";
                vector9Go.transform.localPosition = Vector3.zero;
                vector9Go.transform.localRotation = Quaternion.identity;
                vector9Go.SetActive(false);
            }
            else
            {
                vector9Go = new GameObject("Weapon_Vector9_Fallback");
                vector9Go.transform.SetParent(weaponMountGo.transform);
                vector9Go.transform.localPosition = Vector3.zero;
                vector9Go.SetActive(false);
            }

            var muzzle9 = new GameObject("MuzzlePoint_Vector9");
            muzzle9.transform.SetParent(vector9Go.transform);
            muzzle9.transform.localPosition = new Vector3(0f, 0.04f, 0.28f);

            var vector9Weapon = vector9Go.AddComponent<Vector9Pistol>();
            SetPrivateField(vector9Weapon, "_data", vector9Data);
            SetPrivateField(vector9Weapon, "_muzzlePoint", muzzle9.transform);
            SetPrivateField(vector9Weapon, "_damageChannel", damageChannel);
            SetPrivateField(vector9Weapon, "_killChannel", killChannel);
            SetPrivateField(vector9Weapon, "_acousticChannel", acousticChannel);

            var weaponList = new List<BaseWeapon> { synapseWeapon, vector9Weapon };
            SetPrivateField(combatController, "_weapons", weaponList);
            SetPrivateField(combatController, "_currentWeaponIndex", 0);

            // Player Gadget Controller
            var gadgetController = playerGo.AddComponent<PlayerGadgetController>();
            SetPrivateField(gadgetController, "_playerCamera", cam);
            SetPrivateField(gadgetController, "_throwOrigin", muzzleGo.transform);
            SetPrivateField(gadgetController, "_gadgetChannel", gadgetChannel);
            SetPrivateField(gadgetController, "_inputActions", inputActions);
            SetPrivateField(watchDisplay, "_gadgetController", gadgetController);

            // 7. Tactical Sentinels (FR-22 to FR-27)
            // Sentry 1: Room 1 entry guardian behind Cover Pillar
            var sentinel01 = SpawnSentinel(room01Go.transform, "Sentinel_R1_Sentry", new Vector3(-2f, 0f, 3.5f), Quaternion.Euler(0, 180, 0), playerGo.transform, acousticChannel, radioChannel, killChannel);
            room01.RegisterResettable(sentinel01);

            // Sentry 2 & 3: Room 2 defenders in tactical cover
            var sentinel02 = SpawnSentinel(room02Go.transform, "Sentinel_R2_Flanker", new Vector3(-2f, 0f, 13.5f), Quaternion.Euler(0, 180, 0), playerGo.transform, acousticChannel, radioChannel, killChannel);
            var sentinel03 = SpawnSentinel(room02Go.transform, "Sentinel_R2_Anchor", new Vector3(2.5f, 0f, 17.5f), Quaternion.Euler(0, 180, 0), playerGo.transform, acousticChannel, radioChannel, killChannel);
            room02.RegisterResettable(sentinel02);
            room02.RegisterResettable(sentinel03);

            // Sentry 4 & 5: Room 3 Data Core defenders
            var sentinel04 = SpawnSentinel(room03Go.transform, "Sentinel_R3_CoreGuard_Left", new Vector3(-3.5f, 0f, 32f), Quaternion.Euler(0, 180, 0), playerGo.transform, acousticChannel, radioChannel, killChannel);
            var sentinel05 = SpawnSentinel(room03Go.transform, "Sentinel_R3_CoreGuard_Right", new Vector3(3.5f, 0f, 32f), Quaternion.Euler(0, 180, 0), playerGo.transform, acousticChannel, radioChannel, killChannel);
            room03.RegisterResettable(sentinel04);
            room03.RegisterResettable(sentinel05);
            room03.RegisterResettable(terminal);
            room04.RegisterResettable(gateway);

            // 8. Core Managers & Settings
            var managersRoot = new GameObject("Core_Managers");

            var memDumpGo = new GameObject("MemoryDumpManager");
            memDumpGo.transform.SetParent(managersRoot.transform);
            var memDump = memDumpGo.AddComponent<MemoryDumpManager>();
            SetPrivateField(memDump, "_playerStateEvents", playerStateEvents);
            SetPrivateField(memDump, "_memoryDumpResetEvents", voidResetChannel);
            SetPrivateField(memDump, "_playerLocomotion", locomotion);
            SetPrivateField(memDump, "_playerHealth", playerHealth);
            SetPrivateField(memDump, "_playerCombat", combatController);
            SetPrivateField(memDump, "_playerGadgets", gadgetController);
            SetPrivateField(memDump, "_activeRoom", room01);

            var settingsGo = new GameObject("SettingsManager");
            settingsGo.transform.SetParent(managersRoot.transform);
            var settingsManager = settingsGo.AddComponent<SettingsManager>();
            SetPrivateField(settingsManager, "_inputActions", inputActions);
            SetPrivateField(settingsManager, "_mainCamera", cam);
            SetPrivateField(settingsManager, "_cameraLook", cameraLook);
            SetPrivateField(settingsManager, "_weaponSwayAndAds", swayAndAds);
            SetPrivateField(settingsManager, "_weaponReceiverDisplay", receiverDisplay);
            SetPrivateField(settingsManager, "_forearmWatchDisplay", watchDisplay);

            // 9. Bake NavMesh for environment
            navSurface.BuildNavMesh();

            // 10. Mark scene dirty & save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[ProductionSceneBuilder] Subsector 01 fully assembled with production 3D assets, NavMesh baked, and saved!");
        }

        #region Helper Spawners
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
                    GameObject floor;
                    if (floorPrefab != null)
                    {
                        floor = Object.Instantiate(floorPrefab, parent);
                        floor.transform.position = pos;
                        floor.transform.rotation = Quaternion.identity;
                    }
                    else
                    {
                        floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        floor.transform.SetParent(parent);
                        floor.transform.position = pos + new Vector3(0f, -0.1f, 0f);
                        floor.transform.localScale = new Vector3(4f, 0.2f, 4f);
                    }
                    floor.name = $"Floor_{x}_{z}";
                    EnsureBoxCollider(floor, new Vector3(4f, 0.2f, 4f), new Vector3(0f, -0.1f, 0f));
                }
            }
        }

        private static GameObject SpawnWall(Transform parent, Vector3 pos, Quaternion rot)
        {
            var wallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_WALL);
            GameObject wall;
            if (wallPrefab != null)
            {
                wall = Object.Instantiate(wallPrefab, parent);
                wall.transform.position = pos;
                wall.transform.rotation = rot;
            }
            else
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.SetParent(parent);
                wall.transform.position = pos + rot * new Vector3(0f, 2f, 0f);
                wall.transform.rotation = rot;
                wall.transform.localScale = new Vector3(4f, 4f, 0.4f);
            }
            wall.tag = Tags.GridWall;
            EnsureBoxCollider(wall, new Vector3(4f, 4f, 0.4f), new Vector3(0f, 2f, 0f));
            return wall;
        }

        private static GameObject SpawnDoorframe(Transform parent, Vector3 pos, Quaternion rot)
        {
            var doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_DOORFRAME);
            GameObject frame;
            if (doorPrefab != null)
            {
                frame = Object.Instantiate(doorPrefab, parent);
                frame.transform.position = pos;
                frame.transform.rotation = rot;
            }
            else
            {
                frame = new GameObject("WallDoorframe_Fallback");
                frame.transform.SetParent(parent);
                frame.transform.position = pos;
                frame.transform.rotation = rot;
            }
            frame.tag = Tags.GridWall;
            // Left jamb, right jamb, lintel colliders
            EnsureBoxCollider(frame, new Vector3(0.9f, 3.0f, 0.4f), new Vector3(-1.55f, 1.5f, 0f));
            var rightCol = frame.AddComponent<BoxCollider>();
            rightCol.size = new Vector3(0.9f, 3.0f, 0.4f);
            rightCol.center = new Vector3(1.55f, 1.5f, 0f);
            var lintelCol = frame.AddComponent<BoxCollider>();
            lintelCol.size = new Vector3(4.0f, 1.0f, 0.4f);
            lintelCol.center = new Vector3(0f, 3.5f, 0f);
            return frame;
        }

        private static CoverNode SpawnCoverPillar(Transform parent, Vector3 pos, CoverHeight height, Vector3 normal)
        {
            var pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_COVER_PILLAR);
            GameObject pillar;
            if (pillarPrefab != null)
            {
                pillar = Object.Instantiate(pillarPrefab, parent);
                pillar.transform.position = pos;
                pillar.transform.rotation = Quaternion.identity;
            }
            else
            {
                pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.transform.SetParent(parent);
                pillar.transform.position = pos + new Vector3(0f, 2f, 0f);
                pillar.transform.localScale = new Vector3(1f, 4f, 1f);
            }
            pillar.tag = Tags.GridWall;
            EnsureBoxCollider(pillar, new Vector3(1f, 4f, 1f), new Vector3(0f, 2f, 0f));

            var node = pillar.AddComponent<CoverNode>();
            SetPrivateField(node, "_coverHeight", height);
            SetPrivateField(node, "_coverNormal", normal);
            return node;
        }

        private static CoverNode SpawnLowCover(Transform parent, Vector3 pos, Quaternion rot, CoverHeight height, Vector3 normal)
        {
            var lowCoverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_LOW_COVER);
            GameObject lowCover;
            if (lowCoverPrefab != null)
            {
                lowCover = Object.Instantiate(lowCoverPrefab, parent);
                lowCover.transform.position = pos;
                lowCover.transform.rotation = rot;
            }
            else
            {
                lowCover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lowCover.transform.SetParent(parent);
                lowCover.transform.position = pos + rot * new Vector3(0f, 0.55f, 0f);
                lowCover.transform.rotation = rot;
                lowCover.transform.localScale = new Vector3(4f, 1.1f, 0.4f);
            }
            lowCover.tag = Tags.GridWall;
            EnsureBoxCollider(lowCover, new Vector3(4f, 1.1f, 0.4f), new Vector3(0f, 0.55f, 0f));

            var node = lowCover.AddComponent<CoverNode>();
            SetPrivateField(node, "_coverHeight", height);
            SetPrivateField(node, "_coverNormal", normal);
            return node;
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
            GameObject terminalGo;
            if (terminalPrefab != null)
            {
                terminalGo = Object.Instantiate(terminalPrefab, parent);
                terminalGo.transform.position = pos;
                terminalGo.transform.rotation = rot;
            }
            else
            {
                terminalGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                terminalGo.transform.SetParent(parent);
                terminalGo.transform.position = pos + new Vector3(0f, 0.8f, 0f);
                terminalGo.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
            }
            EnsureBoxCollider(terminalGo, new Vector3(1.2f, 1.6f, 0.8f), new Vector3(0f, 0.8f, 0f));
            var terminal = terminalGo.AddComponent<DataCoreTerminal>();
            return terminal;
        }

        private static ExtractionGateway SpawnExtractionGateway(Transform parent, Vector3 pos, Quaternion rot)
        {
            var gatewayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_GATEWAY);
            GameObject gatewayGo;
            if (gatewayPrefab != null)
            {
                gatewayGo = Object.Instantiate(gatewayPrefab, parent);
                gatewayGo.transform.position = pos;
                gatewayGo.transform.rotation = rot;
            }
            else
            {
                gatewayGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gatewayGo.transform.SetParent(parent);
                gatewayGo.transform.position = pos + new Vector3(0f, 2f, 0f);
                gatewayGo.transform.localScale = new Vector3(4f, 4f, 0.6f);
            }
            var col = EnsureBoxCollider(gatewayGo, new Vector3(4f, 4f, 0.6f), new Vector3(0f, 2f, 0f));
            var gateway = gatewayGo.AddComponent<ExtractionGateway>();
            SetPrivateField(gateway, "_physicalBarrier", col);
            return gateway;
        }

        private static StandardSentinel SpawnSentinel(Transform parent, string name, Vector3 pos, Quaternion rot, Transform playerTransform, AcousticStimulusEventChannelSO acousticChannel, AIRadioChatterEventChannelSO radioChannel, KillEventChannelSO killChannel)
        {
            var sentinelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_SENTINEL);
            GameObject sentinelGo;
            if (sentinelPrefab != null)
            {
                sentinelGo = Object.Instantiate(sentinelPrefab, parent);
                sentinelGo.name = name;
                sentinelGo.transform.position = pos;
                sentinelGo.transform.rotation = rot;
            }
            else
            {
                sentinelGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                sentinelGo.name = name;
                sentinelGo.transform.SetParent(parent);
                sentinelGo.transform.position = pos + new Vector3(0f, 0.9f, 0f);
                sentinelGo.transform.rotation = rot;
            }
            sentinelGo.tag = Tags.Enemy;

            // Character body collider
            var bodyCol = sentinelGo.GetComponent<CapsuleCollider>();
            if (bodyCol == null) bodyCol = sentinelGo.AddComponent<CapsuleCollider>();
            bodyCol.height = 1.8f;
            bodyCol.radius = 0.4f;
            bodyCol.center = new Vector3(0f, 0.9f, 0f);

            var health = sentinelGo.AddComponent<SentinelHealth>();
            SetPrivateField(health, "_killChannel", killChannel);

            // Head hitbox (FR-13: 1-tap headshot kill)
            var headGo = new GameObject("Sentinel_Head");
            headGo.tag = Tags.EnemyHead;
            headGo.transform.SetParent(sentinelGo.transform);
            headGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var headCol = headGo.AddComponent<SphereCollider>();
            headCol.radius = 0.22f;

            // NavMeshAgent
            var navAgent = sentinelGo.GetComponent<NavMeshAgent>();
            if (navAgent == null) navAgent = sentinelGo.AddComponent<NavMeshAgent>();
            navAgent.speed = 2.5f;
            navAgent.stoppingDistance = 4.0f;

            // Perception & Radio
            var perception = sentinelGo.AddComponent<SentinelPerception>();
            SetPrivateField(perception, "_eyeTransform", headGo.transform);
            SetPrivateField(perception, "_acousticChannel", acousticChannel);

            var radio = sentinelGo.AddComponent<SentinelRadioChatter>();
            SetPrivateField(radio, "_radioChannel", radioChannel);

            // Voxel Shatter Death FX (FR-38)
            sentinelGo.AddComponent<SentinelVoxelShatterDeath>();

            // HFSM Core
            var standardSentinel = sentinelGo.AddComponent<StandardSentinel>();
            SetPrivateField(standardSentinel, "_perception", perception);
            SetPrivateField(standardSentinel, "_radioChatter", radio);
            SetPrivateField(standardSentinel, "_health", health);
            SetPrivateField(standardSentinel, "_navAgent", navAgent);
            standardSentinel.SetTargetPlayer(playerTransform);

            return standardSentinel;
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
            synapse.WeaponId = WeaponId.SynapseAR;
            synapse.WeaponName = "Synapse-AR";
            synapse.MagazineCapacity = 20;
            synapse.InitialSpareAmmo = 60;
            synapse.RoundsPerMinute = 550f;
            synapse.BurstRoundsPerMinute = 550f;
            synapse.BurstCount = 3;
            synapse.BodyDamage = 45;
            synapse.HeadDamage = 112;
            synapse.IsSilenced = false;
            synapse.ReloadTime = 1.9f;
            synapse.RecoilPitchAngle = 2.2f;
            synapse.PenetratesBarricades = true;
            EditorUtility.SetDirty(synapse);

            var vector9 = LoadOrCreateSO<WeaponDataSO>($"{WEAPON_DATA_FOLDER}/Vector9_Data.asset");
            vector9.WeaponId = WeaponId.Vector9;
            vector9.WeaponName = "Vector-9";
            vector9.MagazineCapacity = 12;
            vector9.InitialSpareAmmo = 36;
            vector9.RoundsPerMinute = 400f;
            vector9.BodyDamage = 30;
            vector9.HeadDamage = 75;
            vector9.IsSilenced = true;
            vector9.ReloadTime = 1.2f;
            vector9.RecoilPitchAngle = 0.8f;
            EditorUtility.SetDirty(vector9);

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
                Debug.LogWarning($"[ProductionSceneBuilder] Field '{fieldName}' not found on '{target.GetType().Name}'.");
            }
        }
        #endregion
    }
}
