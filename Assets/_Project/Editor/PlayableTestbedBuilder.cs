using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
    /// Editor utility for constructing a fully functional graybox playable testbed scene (FR-28, FR-30, UJ-1 through UJ-5).
    /// Generates modular 4m rooms, calibrated sightlines, full player rig, weapons, gadgets, AI sentinels, and core managers.
    /// </summary>
    public static class PlayableTestbedBuilder
    {
        private const string TESTBED_SCENE_PATH = "Assets/Scenes/SampleScene.unity";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";
        private const string EVENT_CHANNELS_FOLDER = "Assets/_Project/ScriptableObjects/Events";
        private const string WEAPON_DATA_FOLDER = "Assets/_Project/ScriptableObjects/Weapons";

        [MenuItem("NullProtocol/Build Playable Testbed Scene", false, 1)]
        public static void BuildPlayableTestbed()
        {
            // 1. Ensure required assets exist
            EnsureDirectories();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            if (inputActions == null)
            {
                Debug.LogError($"[PlayableTestbedBuilder] Could not find InputActionAsset at '{INPUT_ACTIONS_PATH}'.");
                return;
            }

            var (playerStateEvents, damageChannel, killChannel, acousticChannel, gadgetChannel, voidResetChannel, radioChannel) = EnsureEventChannels();
            var (synapseArData, vector9Data) = EnsureWeaponData();

            // 2. Open or create target scene
            Scene scene = EditorSceneManager.OpenScene(TESTBED_SCENE_PATH, OpenSceneMode.Single);
            var rootObjects = scene.GetRootGameObjects();
            if (rootObjects.Length > 0)
            {
                Undo.RegisterFullObjectHierarchyUndo(rootObjects[0], "Build Playable Testbed");
            }

            // Clear existing non-essential root objects (keep camera/light if desired, but we re-create clean hierarchy)
            for (int i = rootObjects.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(rootObjects[i]);
            }

            // 3. Materials
            var floorMat = CreateColorMaterial("Mat_Graybox_Floor", new Color(0.12f, 0.14f, 0.18f));
            var wallMat = CreateColorMaterial("Mat_Graybox_Wall", new Color(0.2f, 0.24f, 0.3f));
            var coverMat = CreateColorMaterial("Mat_Graybox_Cover", new Color(0.3f, 0.45f, 0.6f));
            var sentinelBodyMat = CreateColorMaterial("Mat_Sentinel_Body", new Color(0.85f, 0.2f, 0.2f));
            var sentinelHeadMat = CreateColorMaterial("Mat_Sentinel_Head", new Color(1.0f, 0.85f, 0.1f));
            var terminalMat = CreateColorMaterial("Mat_DataCore_Terminal", new Color(0.1f, 0.9f, 0.7f));
            var gatewayMat = CreateColorMaterial("Mat_Extraction_Gateway", new Color(0.9f, 0.4f, 0.1f));

            // 4. Environment: Modular Rooms (4m grid, max 12m sightlines)
            var envRoot = new GameObject("Environment_ModularGrid");

            // Lighting & Atmosphere
            var sunGo = new GameObject("Directional Light");
            sunGo.transform.SetParent(envRoot.transform);
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.8f;
            light.color = new Color(0.95f, 0.95f, 1.0f);

            // Subsector Room 01: Infiltration Corridor & Breach Room
            var room01Go = new GameObject("Room_Sector_01");
            room01Go.transform.SetParent(envRoot.transform);
            var room01 = room01Go.AddComponent<RoomController>();
            room01.Initialize("Room_01", "Subsector Alpha Entry");

            var spawnPointGo = new GameObject("PlayerSpawnPoint");
            spawnPointGo.transform.SetParent(room01Go.transform);
            spawnPointGo.transform.position = new Vector3(0f, 0.1f, 0f);
            spawnPointGo.transform.rotation = Quaternion.identity;
            SetPrivateField(room01, "_spawnPoint", spawnPointGo.transform);

            // Floor (16m x 24m)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_4mGrid";
            floor.transform.SetParent(room01Go.transform);
            floor.transform.position = new Vector3(0f, -0.25f, 8f);
            floor.transform.localScale = new Vector3(16f, 0.5f, 24f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Perimeter Walls (4m high)
            CreateWall(room01Go.transform, "Wall_North", new Vector3(0f, 2f, 20f), new Vector3(16f, 4f, 0.5f), wallMat);
            CreateWall(room01Go.transform, "Wall_South", new Vector3(0f, 2f, -4f), new Vector3(16f, 4f, 0.5f), wallMat);
            CreateWall(room01Go.transform, "Wall_West", new Vector3(-8f, 2f, 8f), new Vector3(0.5f, 4f, 24f), wallMat);
            CreateWall(room01Go.transform, "Wall_East", new Vector3(8f, 2f, 8f), new Vector3(0.5f, 4f, 24f), wallMat);

            // Chokepoint Partition Wall (Z = 8) with 4m Doorway opening
            CreateWall(room01Go.transform, "Partition_Left", new Vector3(-5f, 2f, 8f), new Vector3(6f, 4f, 0.5f), wallMat);
            CreateWall(room01Go.transform, "Partition_Right", new Vector3(5f, 2f, 8f), new Vector3(6f, 4f, 0.5f), wallMat);
            CreateWall(room01Go.transform, "Partition_Lintel", new Vector3(0f, 3.5f, 8f), new Vector3(4f, 1f, 0.5f), wallMat);

            // Low Tactical Cover Objects (1.1m high, 1.8m wide for leaning & crouching)
            var cover1 = CreateCoverObstacle(room01Go.transform, "Cover_Low_NearDoor_Left", new Vector3(-2.5f, 0.55f, 5.5f), new Vector3(1.8f, 1.1f, 0.4f), coverMat, CoverHeight.Low, Vector3.forward);
            var cover2 = CreateCoverObstacle(room01Go.transform, "Cover_Low_Room_Center", new Vector3(2.0f, 0.55f, 13.0f), new Vector3(2.0f, 1.1f, 0.4f), coverMat, CoverHeight.Low, -Vector3.forward);
            var cover3 = CreateCoverObstacle(room01Go.transform, "Cover_Pillar_High", new Vector3(-3.0f, 2.0f, 14.0f), new Vector3(1.0f, 4.0f, 1.0f), coverMat, CoverHeight.High, -Vector3.forward);

            // 5. Objectives: Data Core Terminal & Extraction Gateway
            var terminalGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            terminalGo.name = "DataCore_Terminal_Alpha";
            terminalGo.transform.SetParent(room01Go.transform);
            terminalGo.transform.position = new Vector3(5.5f, 1.0f, 16.0f);
            terminalGo.transform.localScale = new Vector3(0.8f, 1.0f, 0.8f);
            terminalGo.GetComponent<Renderer>().sharedMaterial = terminalMat;
            var terminal = terminalGo.AddComponent<DataCoreTerminal>();

            var gatewayGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gatewayGo.name = "Extraction_Gateway_Alpha";
            gatewayGo.transform.SetParent(room01Go.transform);
            gatewayGo.transform.position = new Vector3(0f, 2.0f, 19.5f);
            gatewayGo.transform.localScale = new Vector3(4.0f, 4.0f, 0.5f);
            gatewayGo.GetComponent<Renderer>().sharedMaterial = gatewayMat;
            var gateway = gatewayGo.AddComponent<ExtractionGateway>();
            gateway.RegisterRequiredTerminal(terminal);
            terminal.TargetGateway = gateway;

            // 6. Assembled Player Rig
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

            // Camera Mount & Lean
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

            // Weapon Mount & ADS
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

            // Munitions Economy Manager
            var economyManager = playerGo.AddComponent<MunitionsEconomyManager>();
            SetPrivateField(economyManager, "_killChannel", killChannel);
            SetPrivateField(economyManager, "_gadgetChannel", gadgetChannel);
            SetPrivateField(combatController, "_economyManager", economyManager);

            // Synapse-AR Rifle (Primary)
            var synapseArGo = new GameObject("Weapon_SynapseAR");
            synapseArGo.transform.SetParent(weaponMountGo.transform);
            synapseArGo.transform.localPosition = Vector3.zero;
            synapseArGo.transform.localRotation = Quaternion.identity;

            // Graybox gun model mesh
            var gunMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunMesh.name = "GunMesh";
            gunMesh.transform.SetParent(synapseArGo.transform);
            gunMesh.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            gunMesh.transform.localScale = new Vector3(0.08f, 0.12f, 0.5f);
            var gunCollider = gunMesh.GetComponent<Collider>();
            if (gunCollider != null) Object.DestroyImmediate(gunCollider);

            var muzzleGo = new GameObject("MuzzlePoint");
            muzzleGo.transform.SetParent(synapseArGo.transform);
            muzzleGo.transform.localPosition = new Vector3(0f, 0.03f, 0.45f);

            var receiverDisplay = gunMesh.AddComponent<WeaponReceiverDisplay>();
            SetPrivateField(receiverDisplay, "_receiverRenderer", gunMesh.GetComponent<Renderer>());

            var synapseWeapon = synapseArGo.AddComponent<SynapseAR>();
            SetPrivateField(synapseWeapon, "_data", synapseArData);
            SetPrivateField(synapseWeapon, "_muzzlePoint", muzzleGo.transform);
            SetPrivateField(synapseWeapon, "_damageChannel", damageChannel);
            SetPrivateField(synapseWeapon, "_killChannel", killChannel);
            SetPrivateField(synapseWeapon, "_acousticChannel", acousticChannel);

            // Attach weapon to combat inventory
            var weaponList = new List<BaseWeapon> { synapseWeapon };
            SetPrivateField(combatController, "_weapons", weaponList);
            SetPrivateField(combatController, "_currentWeaponIndex", 0);

            // Forearm Watch Display
            var watchGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            watchGo.name = "Forearm_Watch";
            watchGo.transform.SetParent(mainCamGo.transform);
            watchGo.transform.localPosition = new Vector3(-0.25f, -0.22f, 0.35f);
            watchGo.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
            var watchCollider = watchGo.GetComponent<Collider>();
            if (watchCollider != null) Object.DestroyImmediate(watchCollider);

            var watchDisplay = watchGo.AddComponent<ForearmWatchDisplay>();
            SetPrivateField(watchDisplay, "_oledScreenRenderer", watchGo.GetComponent<Renderer>());
            SetPrivateField(watchDisplay, "_playerStateEvents", playerStateEvents);
            SetPrivateField(watchDisplay, "_gadgetEvents", gadgetChannel);

            // Player Gadget Controller
            var gadgetController = playerGo.AddComponent<PlayerGadgetController>();
            SetPrivateField(gadgetController, "_playerCamera", cam);
            SetPrivateField(gadgetController, "_throwOrigin", muzzleGo.transform);
            SetPrivateField(gadgetController, "_gadgetChannel", gadgetChannel);
            SetPrivateField(gadgetController, "_inputActions", inputActions);
            SetPrivateField(watchDisplay, "_gadgetController", gadgetController);

            // 7. Tactical Sentinel AI Target (Sector defender behind cover)
            var sentinelGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            sentinelGo.name = "Sentinel_Subroutine_01";
            sentinelGo.tag = Tags.Enemy;
            sentinelGo.transform.SetParent(room01Go.transform);
            sentinelGo.transform.position = new Vector3(2.0f, 1.0f, 15.0f);
            sentinelGo.GetComponent<Renderer>().sharedMaterial = sentinelBodyMat;

            var sentinelHealth = sentinelGo.AddComponent<SentinelHealth>();
            SetPrivateField(sentinelHealth, "_killChannel", killChannel);

            // Head Hitbox (1-tap headshot per FR-13)
            var headGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headGo.name = "Sentinel_Head";
            headGo.tag = Tags.EnemyHead;
            headGo.transform.SetParent(sentinelGo.transform);
            headGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            headGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            headGo.GetComponent<Renderer>().sharedMaterial = sentinelHeadMat;

            // NavMeshAgent & Sentinel AI components
            var navAgent = sentinelGo.AddComponent<NavMeshAgent>();
            navAgent.speed = 2.5f;
            navAgent.stoppingDistance = 4.0f;

            var perception = sentinelGo.AddComponent<SentinelPerception>();
            SetPrivateField(perception, "_eyeTransform", headGo.transform);
            SetPrivateField(perception, "_acousticChannel", acousticChannel);

            var radio = sentinelGo.AddComponent<SentinelRadioChatter>();
            SetPrivateField(radio, "_radioChannel", radioChannel);

            var standardSentinel = sentinelGo.AddComponent<StandardSentinel>();
            SetPrivateField(standardSentinel, "_perception", perception);
            SetPrivateField(standardSentinel, "_radioChatter", radio);
            SetPrivateField(standardSentinel, "_health", sentinelHealth);
            SetPrivateField(standardSentinel, "_navAgent", navAgent);
            standardSentinel.SetTargetPlayer(playerGo.transform);

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

            // Register room resettables
            room01.RegisterResettable(standardSentinel);
            room01.RegisterResettable(terminal);
            room01.RegisterResettable(gateway);

            // 9. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[PlayableTestbedBuilder] Successfully built and saved fully functional playable testbed in 'Assets/Scenes/SampleScene.unity'!");
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.tag = Tags.GridWall;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static CoverNode CreateCoverObstacle(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, CoverHeight height, Vector3 normal)
        {
            var coverGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coverGo.name = name;
            coverGo.tag = Tags.GridWall;
            coverGo.transform.SetParent(parent);
            coverGo.transform.position = pos;
            coverGo.transform.localScale = scale;
            coverGo.GetComponent<Renderer>().sharedMaterial = mat;

            var node = coverGo.AddComponent<CoverNode>();
            SetPrivateField(node, "_coverHeight", height);
            SetPrivateField(node, "_coverNormal", normal);
            return node;
        }

        private static Material CreateColorMaterial(string matName, Color color)
        {
            string matPath = $"Assets/_Project/Materials/{matName}.mat";
            if (!Directory.Exists("Assets/_Project/Materials"))
            {
                Directory.CreateDirectory("Assets/_Project/Materials");
            }

            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/_Project/ScriptableObjects/Events"))
                Directory.CreateDirectory("Assets/_Project/ScriptableObjects/Events");
            if (!Directory.Exists("Assets/_Project/ScriptableObjects/Weapons"))
                Directory.CreateDirectory("Assets/_Project/ScriptableObjects/Weapons");
            if (!Directory.Exists("Assets/_Project/Materials"))
                Directory.CreateDirectory("Assets/_Project/Materials");
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
                Debug.LogWarning($"[PlayableTestbedBuilder] Could not find field '{fieldName}' on '{target.GetType().Name}'.");
            }
        }
    }
}
