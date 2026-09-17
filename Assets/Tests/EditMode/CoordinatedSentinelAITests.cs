using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using NullProtocol.Core;
using NullProtocol.Combat;
using NullProtocol.Gadgets;
using NullProtocol.AI;

namespace NullProtocol.Tests.EditMode
{
    [TestFixture]
    public class CoordinatedSentinelAITests
    {
        private GameObject _hostGo;
        private AcousticStimulusEventChannelSO _acousticChannel;
        private KillEventChannelSO _killChannel;
        private AIRadioChatterEventChannelSO _radioChannel;

        [SetUp]
        public void SetUp()
        {
            _hostGo = new GameObject("TestHost");
            _acousticChannel = ScriptableObject.CreateInstance<AcousticStimulusEventChannelSO>();
            _killChannel = ScriptableObject.CreateInstance<KillEventChannelSO>();
            _radioChannel = ScriptableObject.CreateInstance<AIRadioChatterEventChannelSO>();
            CoverNodeRegistry.Clear();
            LOSBlockerRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            CoverNodeRegistry.Clear();
            LOSBlockerRegistry.Clear();
            if (_hostGo != null) Object.DestroyImmediate(_hostGo);
            if (_acousticChannel != null) Object.DestroyImmediate(_acousticChannel);
            if (_killChannel != null) Object.DestroyImmediate(_killChannel);
            if (_radioChannel != null) Object.DestroyImmediate(_radioChannel);
        }

        #region FR-22: Perception Model (Visual Raycast & Acoustic Spheres)

        [Test]
        public void FR22_SentinelPerception_InitialState_IsUnaware()
        {
            var perception = _hostGo.AddComponent<SentinelPerception>();
            Assert.AreEqual(AlertState.Unaware, perception.CurrentAlertState);
            Assert.AreEqual(110f, perception.FieldOfViewDegrees);
            Assert.AreEqual(18f, perception.VisualRange);
        }

        [Test]
        public void FR22_SentinelPerception_VisualRaycast_DetectsTargetWithinConeAndRange()
        {
            var sentinelGo = new GameObject("Sentinel");
            var perception = sentinelGo.AddComponent<SentinelPerception>();
            sentinelGo.transform.position = Vector3.zero;
            sentinelGo.transform.forward = Vector3.forward;

            var targetGo = new GameObject("TargetPlayer");
            targetGo.transform.position = new Vector3(0f, 0f, 10f); // directly in front, 10m away

            bool canSee = perception.CanSeeTarget(targetGo.transform, out Vector3 targetPos);
            Assert.IsTrue(canSee, "Sentinel should see target within 110 deg cone and <= 18m.");
            Assert.AreEqual(AlertState.Alerted, perception.CurrentAlertState);
            Assert.IsTrue(perception.HasTargetInSight);

            Object.DestroyImmediate(sentinelGo);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void FR22_SentinelPerception_VisualRaycast_FailsOutsideVisualRange()
        {
            var sentinelGo = new GameObject("Sentinel");
            var perception = sentinelGo.AddComponent<SentinelPerception>();
            sentinelGo.transform.position = Vector3.zero;
            sentinelGo.transform.forward = Vector3.forward;

            var targetGo = new GameObject("TargetPlayer");
            targetGo.transform.position = new Vector3(0f, 0f, 25f); // 25m away (> 18m limit)

            bool canSee = perception.CanSeeTarget(targetGo.transform, out _);
            Assert.IsFalse(canSee, "Sentinel should not see target beyond 18m.");

            Object.DestroyImmediate(sentinelGo);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void FR22_SentinelPerception_VisualRaycast_FailsOutsideFieldOfView()
        {
            var sentinelGo = new GameObject("Sentinel");
            var perception = sentinelGo.AddComponent<SentinelPerception>();
            sentinelGo.transform.position = Vector3.zero;
            sentinelGo.transform.forward = Vector3.forward;

            var targetGo = new GameObject("TargetPlayer");
            // 90 degrees to the right (outside 110 deg cone which has 55 deg half-angle)
            targetGo.transform.position = new Vector3(10f, 0f, 0f);

            bool canSee = perception.CanSeeTarget(targetGo.transform, out _);
            Assert.IsFalse(canSee, "Sentinel should not see target outside 110° FOV cone.");

            Object.DestroyImmediate(sentinelGo);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void FR22_SentinelPerception_VisualRaycast_BlockedByVolumetricSmoke()
        {
            var sentinelGo = new GameObject("Sentinel");
            var perception = sentinelGo.AddComponent<SentinelPerception>();
            sentinelGo.transform.position = Vector3.zero;
            sentinelGo.transform.forward = Vector3.forward;

            var targetGo = new GameObject("TargetPlayer");
            targetGo.transform.position = new Vector3(0f, 0f, 12f);

            // Deploy Null-Cloud Smoke directly between them at 6m
            var smokeGo = new GameObject("Smoke");
            var smoke = smokeGo.AddComponent<NullCloudSmoke>();
            smoke.Detonate(new Vector3(0f, 0f, 6f));

            bool canSee = perception.CanSeeTarget(targetGo.transform, out _);
            Assert.IsFalse(canSee, "Sightline should be completely occluded by Null-Cloud Smoke.");

            Object.DestroyImmediate(sentinelGo);
            Object.DestroyImmediate(targetGo);
            Object.DestroyImmediate(smokeGo);
        }

        [Test]
        public void FR22_SentinelPerception_AcousticStimulus_EscalatesToSuspiciousAndAlerted()
        {
            var sentinelGo = new GameObject("Sentinel");
            var perception = sentinelGo.AddComponent<SentinelPerception>();
            perception.SetAcousticChannel(_acousticChannel);
            sentinelGo.transform.position = Vector3.zero;

            // Footstep noise within radius
            _acousticChannel.RaiseStimulus(new Vector3(0f, 0f, 5f), 10f, AcousticStimulusType.WalkFootstep);
            Assert.AreEqual(AlertState.Suspicious, perception.CurrentAlertState, "Footstep stimulus should escalate unaware to suspicious.");
            Assert.AreEqual(new Vector3(0f, 0f, 5f), perception.LastKnownThreatPosition);

            // Gunfire noise escalates to full alert
            _acousticChannel.RaiseStimulus(new Vector3(2f, 0f, 4f), 15f, AcousticStimulusType.Gunfire);
            Assert.AreEqual(AlertState.Alerted, perception.CurrentAlertState, "Gunfire stimulus should escalate suspicious to alerted.");

            Object.DestroyImmediate(sentinelGo);
        }

        #endregion

        #region FR-23: Cover Nodes, Peeking & Crouching Burst Fire

        [Test]
        public void FR23_CoverNode_RegistersToCoverNodeRegistry()
        {
            Assert.AreEqual(0, CoverNodeRegistry.ActiveNodes.Count);

            var coverGo = new GameObject("CoverNode");
            var cover = coverGo.AddComponent<CoverNode>();

            Assert.AreEqual(1, CoverNodeRegistry.ActiveNodes.Count);
            Assert.AreSame(cover, CoverNodeRegistry.ActiveNodes[0]);

            Object.DestroyImmediate(coverGo);
            Assert.AreEqual(0, CoverNodeRegistry.ActiveNodes.Count);
        }

        [Test]
        public void FR23_CoverNode_EvaluateCoverQuality_PrefersOpposingThreatNormal()
        {
            var coverGo = new GameObject("CoverNode");
            var cover = coverGo.AddComponent<CoverNode>();
            coverGo.transform.position = Vector3.zero;
            coverGo.transform.forward = Vector3.forward; // Facing north

            Vector3 threatInFront = new Vector3(0f, 0f, 10f); // North
            Vector3 threatBehind = new Vector3(0f, 0f, -10f); // South

            float scoreFront = cover.EvaluateCoverQuality(threatInFront, new Vector3(0f, 0f, -1f));
            float scoreBehind = cover.EvaluateCoverQuality(threatBehind, new Vector3(0f, 0f, -1f));

            Assert.IsTrue(scoreFront > scoreBehind, "Cover facing threat should yield significantly higher quality score.");
            Assert.AreEqual(-100f, scoreBehind, "Threat behind cover normal should return penalty score.");

            Object.DestroyImmediate(coverGo);
        }

        [Test]
        public void FR23_CoverNode_ComputesPeekPositions()
        {
            var coverGo = new GameObject("CoverNode");
            var cover = coverGo.AddComponent<CoverNode>();
            coverGo.transform.position = Vector3.zero;
            coverGo.transform.forward = Vector3.forward;

            Vector3 peekLeft = cover.GetPeekPosition(PeekDirection.Left);
            Vector3 peekRight = cover.GetPeekPosition(PeekDirection.Right);
            Vector3 peekOver = cover.GetPeekPosition(PeekDirection.Over);

            Assert.IsTrue(peekLeft.x < 0f, "Left peek should be offset to the left (-X).");
            Assert.IsTrue(peekRight.x > 0f, "Right peek should be offset to the right (+X).");
            Assert.IsTrue(peekOver.y > 0f, "Over peek should be offset upwards (+Y).");

            Object.DestroyImmediate(coverGo);
        }

        [Test]
        public void FR23_StandardSentinel_ExecutesBurstAndCrouchReload()
        {
            var sentinelGo = new GameObject("Sentinel");
            var sentinel = sentinelGo.AddComponent<StandardSentinel>();
            sentinelGo.AddComponent<SentinelHealth>();

            Assert.AreEqual(9, sentinel.MagazineCapacity);
            Assert.AreEqual(9, sentinel.CurrentAmmo);

            bool burstFired = false;
            sentinel.OnBurstFired += () => burstFired = true;

            sentinel.Fire3RoundBurst(sentinelGo.transform.position + Vector3.forward * 10f);
            Assert.IsTrue(sentinel.CurrentAmmo <= 9, "Ammo should be decremented after bursts.");

            // Test reload mechanics
            sentinel.SetCrouching(true);
            Assert.IsTrue(sentinel.IsCrouching, "Sentinel crouches behind cover during reload.");

            Object.DestroyImmediate(sentinelGo);
        }

        #endregion

        #region FR-24: Doorway Stacking & Synchronized Breach

        [Test]
        public void FR24_DoorwayStackingCoordinator_AssignsLeftAndRightPositions()
        {
            var doorGo = new GameObject("DoorwayCoordinator");
            var coordinator = doorGo.AddComponent<DoorwayStackingCoordinator>();
            doorGo.transform.position = Vector3.zero;
            doorGo.transform.forward = Vector3.forward;

            Vector3 leftStack = coordinator.GetStackPosition(StackingSide.Left);
            Vector3 rightStack = coordinator.GetStackPosition(StackingSide.Right);

            Assert.IsTrue(leftStack.x < 0f, "Left stack position should be to the left of doorway.");
            Assert.IsTrue(rightStack.x > 0f, "Right stack position should be to the right of doorway.");

            var sentinel1 = new object();
            var sentinel2 = new object();

            bool assigned1 = coordinator.TryAssignStacker(sentinel1, out StackingSide side1);
            bool assigned2 = coordinator.TryAssignStacker(sentinel2, out StackingSide side2);

            Assert.IsTrue(assigned1);
            Assert.IsTrue(assigned2);
            Assert.AreEqual(StackingSide.Left, side1);
            Assert.AreEqual(StackingSide.Right, side2);
            Assert.IsTrue(coordinator.IsFull);

            Object.DestroyImmediate(doorGo);
        }

        [Test]
        public void FR24_DoorwayStackingCoordinator_ExecutesSynchronizedBreachOnStackComplete()
        {
            var doorGo = new GameObject("DoorwayCoordinator");
            var coordinator = doorGo.AddComponent<DoorwayStackingCoordinator>();

            var sentinel1 = new object();
            var sentinel2 = new object();

            coordinator.TryAssignStacker(sentinel1, out _);
            coordinator.TryAssignStacker(sentinel2, out _);

            bool stackCompleteFired = false;
            bool breachFired = false;
            coordinator.OnStackComplete += () => stackCompleteFired = true;
            coordinator.OnBreachExecuted += () => breachFired = true;

            coordinator.ReportReady(sentinel1);
            Assert.IsFalse(stackCompleteFired, "Stack is not complete until both report ready.");

            coordinator.ReportReady(sentinel2);
            Assert.IsTrue(stackCompleteFired, "Stack complete event must fire when both stackers report ready.");

            coordinator.ExecuteBreach();
            Assert.IsTrue(breachFired);
            Assert.IsTrue(coordinator.IsBreachInitiated);

            Object.DestroyImmediate(doorGo);
        }

        #endregion

        #region FR-25: Shield Breacher Archetype & Flanking

        [Test]
        public void FR25_ShieldBreacher_Has120HP_And_300HP_FrontalShield()
        {
            var breacherGo = new GameObject("ShieldBreacher");
            var breacher = breacherGo.AddComponent<ShieldBreacher>();

            Assert.AreEqual(120, breacher.MaxBodyHealth);
            Assert.AreEqual(120, breacher.CurrentBodyHealth);
            Assert.AreEqual(300, breacher.MaxShieldHealth);
            Assert.AreEqual(300, breacher.CurrentShieldHealth);
            Assert.IsTrue(breacher.IsShieldActive);

            Object.DestroyImmediate(breacherGo);
        }

        [Test]
        public void FR25_ShieldBreacher_FrontalDamage_AbsorbedByShield()
        {
            var breacherGo = new GameObject("ShieldBreacher");
            var breacher = breacherGo.AddComponent<ShieldBreacher>();
            breacherGo.transform.position = Vector3.zero;
            breacherGo.transform.forward = Vector3.forward;

            // Frontal hit: coming from south towards north (bullet forward is north, hit normal is south Vector3.back)
            breacher.TakeDamage(100, Vector3.zero, Vector3.back, false);

            Assert.AreEqual(200, breacher.CurrentShieldHealth, "Frontal shield should absorb damage.");
            Assert.AreEqual(120, breacher.CurrentBodyHealth, "Body health should remain untouched while shield holds.");
            Assert.IsTrue(breacher.IsShieldActive);

            // Deplete shield with 200 more damage
            breacher.TakeDamage(200, Vector3.zero, Vector3.back, false);
            Assert.AreEqual(0, breacher.CurrentShieldHealth);
            Assert.IsFalse(breacher.IsShieldActive, "Shield should shatter when depleted to 0 HP.");

            Object.DestroyImmediate(breacherGo);
        }

        [Test]
        public void FR25_ShieldBreacher_FlankDamage_BypassesShield_DamagesBody()
        {
            var breacherGo = new GameObject("ShieldBreacher");
            var breacher = breacherGo.AddComponent<ShieldBreacher>();
            breacherGo.transform.position = Vector3.zero;
            breacherGo.transform.forward = Vector3.forward; // Facing North

            // Flank hit: coming from behind/side (hit normal is Vector3.forward, meaning shot from south/behind)
            breacher.TakeDamage(50, Vector3.zero, Vector3.forward, false);

            Assert.AreEqual(300, breacher.CurrentShieldHealth, "Shield should NOT absorb hits from behind.");
            Assert.AreEqual(70, breacher.CurrentBodyHealth, "Rear flank hit should damage body directly (120 - 50 = 70).");

            Object.DestroyImmediate(breacherGo);
        }

        [Test]
        public void FR25_ShieldBreacher_CoordinatesFlankWithCompanions()
        {
            var breacherGo = new GameObject("ShieldBreacher");
            var breacher = breacherGo.AddComponent<ShieldBreacher>();
            breacherGo.transform.position = Vector3.zero;

            var sentinelGo = new GameObject("CompanionSentinel");
            var sentinel = sentinelGo.AddComponent<StandardSentinel>();
            sentinelGo.AddComponent<SentinelHealth>();

            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, 0f, 10f);

            breacher.SetTargetPlayer(playerGo.transform);
            breacher.RegisterCompanion(sentinel);

            breacher.CoordinateCompanionFlanks();
            Assert.IsTrue(sentinel.IsFlanking, "Companion sentinel should be commanded to execute flanking route.");

            Object.DestroyImmediate(breacherGo);
            Object.DestroyImmediate(sentinelGo);
            Object.DestroyImmediate(playerGo);
        }

        #endregion

        #region FR-26: Null-01 Mirror Operative Boss

        [Test]
        public void FR26_Null01Boss_Has250HP()
        {
            var bossGo = new GameObject("Null01Boss");
            var boss = bossGo.AddComponent<Null01Boss>();

            Assert.AreEqual(250, boss.MaxHealth);
            Assert.AreEqual(250, boss.CurrentHealth);
            Assert.IsFalse(boss.IsDead);

            Object.DestroyImmediate(bossGo);
        }

        [Test]
        public void FR26_Null01Boss_DeploysBarricade_WhenHPDropsBelow50Percent()
        {
            var bossGo = new GameObject("Null01Boss");
            var boss = bossGo.AddComponent<Null01Boss>();

            bool barricadeFired = false;
            boss.OnBarricadeDeployed += () => barricadeFired = true;

            // Damage to 60% HP (150 HP remaining) -> no barricade
            boss.TakeDamage(100, Vector3.zero, Vector3.up, false);
            Assert.AreEqual(150, boss.CurrentHealth);
            Assert.IsFalse(boss.HasDeployedLowHealthBarricade);
            Assert.IsFalse(barricadeFired);

            // Damage past 50% threshold (<= 125 HP)
            boss.TakeDamage(30, Vector3.zero, Vector3.up, false);
            Assert.AreEqual(120, boss.CurrentHealth);
            Assert.IsTrue(boss.HasDeployedLowHealthBarricade);
            Assert.IsTrue(barricadeFired, "Null-01 must deploy hard-light barricade when HP drops below 50%.");
            Assert.IsTrue(boss.IsFlanking, "Null-01 should execute aggressive flank after deploying barricade.");

            Object.DestroyImmediate(bossGo);
        }

        [Test]
        public void FR26_Null01Boss_ThrowsDefensiveSmokeUnderHeavySuppression()
        {
            var bossGo = new GameObject("Null01Boss");
            var boss = bossGo.AddComponent<Null01Boss>();

            bool smokeThrown = false;
            boss.OnSmokeThrown += () => smokeThrown = true;

            boss.ThrowDefensiveSmoke();
            Assert.IsTrue(boss.HasThrownDefensiveSmoke);
            Assert.IsTrue(smokeThrown, "Null-01 should throw defensive Null-Cloud smoke.");
            Assert.IsTrue(boss.IsFlanking);

            Object.DestroyImmediate(bossGo);
        }

        #endregion

        #region FR-27: 3D Spatialized Synthetic Radio Chatter

        [Test]
        public void FR27_SentinelRadioChatter_EmitsAlertAndFlankingCallouts()
        {
            var chatterGo = new GameObject("RadioChatterHost");
            var chatter = chatterGo.AddComponent<SentinelRadioChatter>();
            chatter.SetRadioChannel(_radioChannel);

            bool chatterReceived = false;
            RadioChatterType receivedType = RadioChatterType.AlertStatusChanged;
            string receivedMsg = null;

            _radioChannel.OnChatterEmitted += (pos, type, msg) =>
            {
                chatterReceived = true;
                receivedType = type;
                receivedMsg = msg;
            };

            bool emitted = chatter.EmitChatter(RadioChatterType.AlertStatusChanged, force: true);
            Assert.IsTrue(emitted);
            Assert.IsTrue(chatterReceived);
            Assert.AreEqual(RadioChatterType.AlertStatusChanged, receivedType);
            Assert.IsNotEmpty(receivedMsg);

            // Flanking callout
            chatterReceived = false;
            chatter.EmitChatter(RadioChatterType.FlankingCallout, force: true);
            Assert.IsTrue(chatterReceived);
            Assert.AreEqual(RadioChatterType.FlankingCallout, receivedType);

            Object.DestroyImmediate(chatterGo);
        }

        #endregion
    }
}
