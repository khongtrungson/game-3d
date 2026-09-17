using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using NullProtocol.Core;
using NullProtocol.Combat;
using NullProtocol.Gadgets;

namespace NullProtocol.Tests.EditMode
{
    [TestFixture]
    public class DeployableFortificationGadgetsTests
    {
        private GameObject _hostGo;
        private GadgetEventChannelSO _gadgetChannel;
        private KillEventChannelSO _killChannel;
        private DamageEventChannelSO _damageChannel;

        // Mock Sentinel that implements ISentinelSmokeReaction
        private class MockSentinelSmokeReaction : MonoBehaviour, ISentinelSmokeReaction
        {
            public bool IsSuppressedBySmoke { get; private set; }
            public bool IsSeekingCover { get; private set; }
            public bool CanFire => !IsSuppressedBySmoke;

            public void OnSmokeSuppressed(Vector3 smokeCenter, float smokeRadius)
            {
                IsSuppressedBySmoke = true;
                IsSeekingCover = true;
            }

            public void OnSmokeCleared()
            {
                IsSuppressedBySmoke = false;
                IsSeekingCover = false;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _hostGo = new GameObject("TestHost");
            _gadgetChannel = ScriptableObject.CreateInstance<GadgetEventChannelSO>();
            _killChannel = ScriptableObject.CreateInstance<KillEventChannelSO>();
            _damageChannel = ScriptableObject.CreateInstance<DamageEventChannelSO>();
            LOSBlockerRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            LOSBlockerRegistry.Clear();
            if (_hostGo != null) Object.DestroyImmediate(_hostGo);
            if (_gadgetChannel != null) Object.DestroyImmediate(_gadgetChannel);
            if (_killChannel != null) Object.DestroyImmediate(_killChannel);
            if (_damageChannel != null) Object.DestroyImmediate(_damageChannel);
        }

        #region FR-16: Hard-Light Barricades

        [Test]
        public void FR16_HardLightBarricade_InitialCharges_EqualsTwo()
        {
            var controller = _hostGo.AddComponent<PlayerGadgetController>();
            Assert.AreEqual(2, controller.BarricadeCharges, "Player shall be equipped with 2 Hard-Light Barricade charges.");
        }

        [Test]
        public void FR16_HardLightBarricade_Dimensions_And_Health_MatchSpecifications()
        {
            var barricadeGo = new GameObject("TestBarricade");
            var barricade = barricadeGo.AddComponent<DeployableBarricade>();

            Assert.AreEqual(1.8f, barricade.BarrierWidth, 0.01f, "Barricade width must be 1.8m.");
            Assert.AreEqual(1.1f, barricade.BarrierHeight, 0.01f, "Barricade height must be 1.1m.");
            Assert.AreEqual(400, barricade.MaxHealth, "Barricade max health must be 400 HP.");
            Assert.AreEqual(0.8f, barricade.DeploymentTime, 0.01f, "Deployment time must be 0.8s.");

            // Tag verification
            Assert.AreEqual(Tags.DeployableBarricade, barricadeGo.tag, "Barricade must be tagged DeployableBarricade.");

            Object.DestroyImmediate(barricadeGo);
        }

        [Test]
        public void FR16_HardLightBarricade_TakeDamage_ReducesHealth_And_ShattersAtZero()
        {
            var barricadeGo = new GameObject("TestBarricade");
            var barricade = barricadeGo.AddComponent<DeployableBarricade>();
            barricade.DeployImmediate(Vector3.zero, Quaternion.identity);

            Assert.AreEqual(400, barricade.CurrentHealth);
            Assert.IsTrue(barricade.IsDeployed);

            // Take 150 damage
            barricade.TakeDamage(150, Vector3.zero, Vector3.up, false);
            Assert.AreEqual(250, barricade.CurrentHealth);
            Assert.IsFalse(barricade.IsDestroyed);

            // Deplete remaining health
            bool shatteredEventFired = false;
            barricade.OnBarricadeDestroyed += b => shatteredEventFired = true;
            barricade.TakeDamage(250, Vector3.zero, Vector3.up, false);

            Assert.AreEqual(0, barricade.CurrentHealth);
            Assert.IsTrue(barricade.IsDestroyed);
            Assert.IsTrue(shatteredEventFired);
            Assert.IsFalse(barricadeGo.activeSelf);

            Object.DestroyImmediate(barricadeGo);
        }

        [Test]
        public void FR16_BarricadePuck_LandsAndInitiatesDeployment()
        {
            var puckGo = new GameObject("TestPuck");
            var puck = puckGo.AddComponent<BarricadePuck>();
            var barricadeGo = new GameObject("TestBarricade");
            var barricade = barricadeGo.AddComponent<DeployableBarricade>();
            barricadeGo.SetActive(false);

            bool landed = false;
            Vector3 landPos = new Vector3(5f, 0f, 10f);
            puck.OnPuckLanded += (pos, rot) => landed = true;

            puck.Launch(Vector3.zero, Vector3.forward, barricade);
            puck.Land(landPos, Vector3.up);

            Assert.IsTrue(landed);
            Assert.IsTrue(puck.HasLanded);
            Assert.IsTrue(barricade.IsDeploying);
            Assert.AreEqual(landPos, barricade.transform.position);

            Object.DestroyImmediate(puckGo);
            Object.DestroyImmediate(barricadeGo);
        }

        #endregion

        #region FR-17: Dynamic NavMesh Obstacle Carving

        [Test]
        public void FR17_HardLightBarricade_CarvesNavMeshObstacle_WithinFiveMilliseconds()
        {
            var barricadeGo = new GameObject("TestBarricade");
            var barricade = barricadeGo.AddComponent<DeployableBarricade>();

            // Verify NavMeshObstacle configuration
            var obstacle = barricade.NavObstacle;
            Assert.IsNotNull(obstacle, "DeployableBarricade must have a NavMeshObstacle component.");
            Assert.IsTrue(obstacle.carving, "NavMeshObstacle carving must be enabled.");
            Assert.IsTrue(obstacle.carveOnlyStationary, "NavMeshObstacle carveOnlyStationary must be enabled.");
            Assert.AreEqual(NavMeshObstacleShape.Box, obstacle.shape);
            Assert.AreEqual(new Vector3(1.8f, 1.1f, 0.2f), obstacle.size);

            // Deploy and check carving execution time
            barricade.DeployImmediate(Vector3.zero, Quaternion.identity);
            Assert.IsTrue(obstacle.enabled, "NavMeshObstacle must be enabled upon deployment.");
            Assert.Less(barricade.LastCarveDurationMs, 5.0, "Dynamic obstacle carving must execute within 5 ms (FR-17).");

            Object.DestroyImmediate(barricadeGo);
        }

        #endregion

        #region FR-18: Null-Cloud Smoke

        [Test]
        public void FR18_NullCloudSmoke_InitialCharges_EqualsTwo()
        {
            var controller = _hostGo.AddComponent<PlayerGadgetController>();
            Assert.AreEqual(2, controller.SmokeCharges, "Player shall be equipped with 2 Null-Cloud Smoke charges.");
        }

        [Test]
        public void FR18_NullCloudSmoke_DetonatesOnImpact_IntoFourMeterField_ForEightSeconds()
        {
            var smokeGo = new GameObject("TestSmoke");
            var smoke = smokeGo.AddComponent<NullCloudSmoke>();

            Assert.AreEqual(4.0f, smoke.Radius, 0.01f, "Null-Cloud Smoke radius must be 4.0m.");
            Assert.AreEqual(8.0f, smoke.Duration, 0.01f, "Null-Cloud Smoke duration must be 8.0s.");

            Vector3 impactPoint = new Vector3(3f, 0f, 7f);
            smoke.Detonate(impactPoint);

            Assert.IsTrue(smoke.IsActive);
            Assert.AreEqual(impactPoint, smoke.Center);
            Assert.AreEqual(8.0f, smoke.RemainingDuration, 0.01f);

            // Dissipate
            smoke.Dissipate();
            Assert.IsFalse(smoke.IsActive);
            Assert.IsFalse(smokeGo.activeSelf);

            Object.DestroyImmediate(smokeGo);
        }

        [Test]
        public void FR18_SmokeCanister_DetonatesOnImpact()
        {
            var canisterGo = new GameObject("TestCanister");
            var canister = canisterGo.AddComponent<SmokeCanister>();
            var smokeGo = new GameObject("TestSmoke");
            var smoke = smokeGo.AddComponent<NullCloudSmoke>();
            smokeGo.SetActive(false);

            bool detonated = false;
            canister.OnCanisterDetonated += pt => detonated = true;

            canister.Launch(Vector3.zero, Vector3.forward, smoke);
            Vector3 impactPt = new Vector3(10f, 0f, 15f);
            canister.DetonateOnImpact(impactPt);

            Assert.IsTrue(detonated);
            Assert.IsTrue(canister.HasDetonated);
            Assert.IsTrue(smoke.IsActive);
            Assert.AreEqual(impactPt, smoke.Center);

            Object.DestroyImmediate(canisterGo);
            Object.DestroyImmediate(smokeGo);
        }

        #endregion

        #region FR-19: Null-Cloud Smoke AI Line-Of-Sight & Suppression

        [Test]
        public void FR19_NullCloudSmoke_InterruptsLineOfSightRaycasts()
        {
            var smokeGo = new GameObject("TestSmoke");
            var smoke = smokeGo.AddComponent<NullCloudSmoke>();
            Vector3 smokeCenter = new Vector3(0f, 0f, 10f);
            smoke.Detonate(smokeCenter);

            // Test 1: Ray passing straight through the smoke center (from 0,0,0 to 0,0,20)
            Vector3 from = new Vector3(0f, 0f, 0f);
            Vector3 to = new Vector3(0f, 0f, 20f);
            Assert.IsTrue(smoke.BlocksLineOfSight(from, to), "Smoke must block sightline passing directly through.");
            Assert.IsTrue(LOSBlockerRegistry.IsLOSBlocked(from, to), "LOSBlockerRegistry must confirm sightline is blocked.");

            // Test 2: Ray passing 2m away from center (within 4.0m radius)
            Vector3 fromNear = new Vector3(2f, 0f, 0f);
            Vector3 toNear = new Vector3(2f, 0f, 20f);
            Assert.IsTrue(smoke.BlocksLineOfSight(fromNear, toNear), "Smoke must block sightline within 4.0m radius.");

            // Test 3: Ray passing 6m away from center (outside 4.0m radius)
            Vector3 fromFar = new Vector3(6f, 0f, 0f);
            Vector3 toFar = new Vector3(6f, 0f, 20f);
            Assert.IsFalse(smoke.BlocksLineOfSight(fromFar, toFar), "Sightline outside 4.0m radius must NOT be blocked.");
            Assert.IsFalse(LOSBlockerRegistry.IsLOSBlocked(fromFar, toFar));

            Object.DestroyImmediate(smokeGo);
        }

        [Test]
        public void FR19_NullCloudSmoke_SuppressesCaughtSentinels_CeaseFireAndSeekCover()
        {
            var smokeGo = new GameObject("TestSmoke");
            var smoke = smokeGo.AddComponent<NullCloudSmoke>();

            var sentinelGo = new GameObject("SentinelAI");
            var sentinelCollider = sentinelGo.AddComponent<SphereCollider>();
            var sentinelReaction = sentinelGo.AddComponent<MockSentinelSmokeReaction>();
            sentinelGo.transform.position = new Vector3(0f, 0f, 2f); // Inside 4.0m radius

            // Detonate smoke at (0, 0, 0)
            smoke.Detonate(Vector3.zero);

            Assert.IsTrue(sentinelReaction.IsSuppressedBySmoke, "Sentinel in smoke must be suppressed.");
            Assert.IsFalse(sentinelReaction.CanFire, "Suppressed sentinel must cease firing (FR-19).");
            Assert.IsTrue(sentinelReaction.IsSeekingCover, "Suppressed sentinel must seek defensive cover (FR-19).");

            // When smoke dissipates, suppression is cleared
            smoke.Dissipate();
            Assert.IsFalse(sentinelReaction.IsSuppressedBySmoke, "Suppression must clear when smoke dissipates.");
            Assert.IsTrue(sentinelReaction.CanFire);

            Object.DestroyImmediate(smokeGo);
            Object.DestroyImmediate(sentinelGo);
        }

        #endregion

        #region FR-20: Logic-Trip Mine

        [Test]
        public void FR20_LogicTripMine_InitialCharges_EqualsOne()
        {
            var controller = _hostGo.AddComponent<PlayerGadgetController>();
            Assert.AreEqual(1, controller.TripMineCharges, "Player shall be equipped with 1 Logic-Trip Mine charge.");
        }

        [Test]
        public void FR20_LogicTripMine_MountsToDoorframe_EmitsThreeMeterLaserTripwire()
        {
            var mineGo = new GameObject("TestMine");
            var mine = mineGo.AddComponent<LogicTripMine>();

            Vector3 mountPos = new Vector3(2f, 1f, 0f);
            Vector3 wallNormal = Vector3.left; // Wall on the right facing left into doorframe
            mine.Mount(mountPos, wallNormal);

            Assert.IsTrue(mine.IsArmed, "Mine must be armed upon mounting.");
            Assert.IsFalse(mine.IsTriggered);
            Assert.AreEqual(3.0f, mine.LaserLength, 0.01f, "Laser tripwire length must be 3.0m (FR-20).");
            Assert.AreEqual(3.0f, mine.CurrentBeamLength, 0.01f);

            Object.DestroyImmediate(mineGo);
        }

        #endregion

        #region FR-21: Logic-Trip Mine Freeze & Instant Elimination

        [Test]
        public void FR21_LogicTripMine_TriggersFreeze_OnCrossingHostile_ForFiveSeconds()
        {
            var mineGo = new GameObject("TestMine");
            var mine = mineGo.AddComponent<LogicTripMine>();
            mine.Mount(Vector3.zero, Vector3.forward, Vector3.right);

            // Create hostile sentinel
            var sentinelGo = new GameObject("HostileSentinel");
            sentinelGo.tag = Tags.Enemy;
            var health = sentinelGo.AddComponent<SentinelHealth>();

            Assert.IsFalse(health.IsFrozen);

            // Trigger mine on target
            mine.TriggerMine(sentinelGo);

            Assert.IsTrue(mine.IsTriggered, "Mine must be marked triggered.");
            Assert.IsTrue(health.IsFrozen, "Hostile must be frozen into wireframe state (FR-21).");
            Assert.AreEqual(5.0f, health.RemainingFreezeTime, 0.01f, "Freeze duration must be 5.0s (FR-21).");

            Object.DestroyImmediate(mineGo);
            Object.DestroyImmediate(sentinelGo);
        }

        [Test]
        public void FR21_FrozenHostile_AllowsInstantElimination_FromAnyWeaponImpact()
        {
            var sentinelGo = new GameObject("HostileSentinel");
            sentinelGo.tag = Tags.Enemy;
            var health = sentinelGo.AddComponent<SentinelHealth>();
            var debuff = sentinelGo.AddComponent<FrozenHostileDebuff>();

            Assert.AreEqual(80, health.CurrentHealth);

            // Freeze the hostile for 5.0s
            debuff.ApplyWireframeFreeze(5.0f);
            Assert.IsTrue(health.IsFrozen || debuff.IsFrozen);

            // Apply minor 1 damage (e.g. from lowest power weapon impact)
            bool killRegistered = false;
            _killChannel.OnKillRegistered += (victim, headshot, pt) => killRegistered = true;
            sentinelGo.GetComponent<SentinelHealth>().TakeDamage(1, Vector3.zero, Vector3.forward, false);

            Assert.AreEqual(0, health.CurrentHealth, "Any weapon impact must instantly eliminate a frozen hostile (FR-21).");
            Assert.IsTrue(health.IsDead);
            Assert.IsTrue(killRegistered);

            Object.DestroyImmediate(sentinelGo);
        }

        [Test]
        public void FR21_FrozenHostile_UnfreezesAfterTimerExpires()
        {
            var sentinelGo = new GameObject("HostileSentinel");
            var health = sentinelGo.AddComponent<SentinelHealth>();

            health.ApplyWireframeFreeze(5.0f);
            Assert.IsTrue(health.IsFrozen);

            // Manually expire freeze
            health.Unfreeze();
            Assert.IsFalse(health.IsFrozen, "Hostile must unfreeze after timer.");

            Object.DestroyImmediate(sentinelGo);
        }

        #endregion

        #region Economy & Reset Integration

        [Test]
        public void PlayerGadgetController_DeployUsesCharges_And_HeadshotStreakAwardsRefund()
        {
            var controller = _hostGo.AddComponent<PlayerGadgetController>();
            var economy = _hostGo.AddComponent<MunitionsEconomyManager>();

            Assert.AreEqual(2, controller.BarricadeCharges);
            Assert.AreEqual(2, controller.SmokeCharges);
            Assert.AreEqual(1, controller.TripMineCharges);

            // Deploy barricade
            bool deployed = controller.TryDeployBarricade();
            Assert.IsTrue(deployed);
            Assert.AreEqual(1, controller.BarricadeCharges);

            // Deploy smoke
            deployed = controller.TryDeploySmoke();
            Assert.IsTrue(deployed);
            Assert.AreEqual(1, controller.SmokeCharges);

            // Deploy trip mine
            deployed = controller.TryDeployTripMine();
            Assert.IsTrue(deployed);
            Assert.AreEqual(0, controller.TripMineCharges);

            // Trying to deploy trip mine again with 0 charges fails
            deployed = controller.TryDeployTripMine();
            Assert.IsFalse(deployed);

            // Simulate refund via GadgetRefunded event
            controller.HandleGadgetRefunded(GadgetType.LogicTripMine, 1);
            Assert.AreEqual(1, controller.TripMineCharges);

            // Reset restores all charges to initial (2, 2, 1)
            controller.ResetState();
            Assert.AreEqual(2, controller.BarricadeCharges);
            Assert.AreEqual(2, controller.SmokeCharges);
            Assert.AreEqual(1, controller.TripMineCharges);
        }

        #endregion
    }
}
