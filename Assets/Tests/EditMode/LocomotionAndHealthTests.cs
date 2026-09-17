using NUnit.Framework;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.Controller;

namespace NullProtocol.Tests.EditMode
{
    [TestFixture]
    public class LocomotionAndHealthTests
    {
        private GameObject _playerGo;
        private TacticalLocomotionController _controller;
        private PlayerHealth _health;
        private CharacterController _characterController;
        private CameraLeanController _leanController;
        private LocomotionAcousticEmitter _acousticEmitter;
        private GameObject _cameraMountGo;

        [SetUp]
        public void SetUp()
        {
            _playerGo = new GameObject("TestPlayer");
            _characterController = _playerGo.AddComponent<CharacterController>();
            _controller = _playerGo.AddComponent<TacticalLocomotionController>();
            _health = _playerGo.AddComponent<PlayerHealth>();
            _acousticEmitter = _playerGo.AddComponent<LocomotionAcousticEmitter>();

            _cameraMountGo = new GameObject("CameraMount");
            _cameraMountGo.transform.SetParent(_playerGo.transform);
            _cameraMountGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            _leanController = _cameraMountGo.AddComponent<CameraLeanController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerGo != null)
            {
                Object.DestroyImmediate(_playerGo);
            }
        }

        [Test]
        public void FR1_BaseWalkSpeed_IsFourMetersPerSecond()
        {
            Assert.AreEqual(4.0f, _controller.WalkSpeed, 0.001f);
        }

        [Test]
        public void FR2_CrouchSpeed_IsTwoPointTwoMetersPerSecond_AndRecoilModifierIsReducedByThirtyPercent()
        {
            Assert.AreEqual(2.2f, _controller.CrouchSpeed, 0.001f);
            
            // Uncrouched recoil modifier should be 1.0
            Assert.AreEqual(1.0f, _controller.RecoilModifier, 0.001f);

            // Crouch
            _controller.SetCrouch(true);
            Assert.IsTrue(_controller.IsCrouching);
            // FR-2: 30% recoil reduction -> 0.7 modifier
            Assert.AreEqual(0.7f, _controller.RecoilModifier, 0.001f);
        }

        [Test]
        public void FR3_TacticalSprintSpeed_IsSixPointTwoMetersPerSecond()
        {
            Assert.AreEqual(6.2f, _controller.SprintSpeed, 0.001f);
        }

        [Test]
        public void FR5_FR6_CameraLean_InitialParameters_MatchSpecification()
        {
            Assert.AreEqual(18.0f, _leanController.MaxLeanAngle, 0.001f);
            Assert.AreEqual(0.35f, _leanController.MaxLeanOffset, 0.001f);
            Assert.AreEqual(0.15f, _leanController.LeanTransitionDuration, 0.001f);

            // Test lean input right (E)
            _leanController.UpdateLeanInput(1.0f);
            // Simulate 0.3s (greater than 0.15s transition)
            for (int i = 0; i < 30; i++)
            {
                _leanController.TickLean(0.01f);
            }

            Assert.Greater(_leanController.CurrentLeanFactor, 0.9f);
            Assert.LessOrEqual(_leanController.CurrentCameraOffset.x, 0.35f + 0.001f);
            Assert.Greater(_leanController.CurrentCameraOffset.x, 0.30f);
            Assert.AreEqual(-18.0f * _leanController.CurrentLeanFactor, _leanController.CurrentRollAngle, 0.1f);
        }

        [Test]
        public void FR7_AcousticStimulusRadii_MatchSpecification()
        {
            Assert.AreEqual(1.5f, _acousticEmitter.CrouchRadius, 0.001f);
            Assert.AreEqual(6.0f, _acousticEmitter.WalkRadius, 0.001f);
            Assert.AreEqual(18.0f, _acousticEmitter.SprintRadius, 0.001f);
        }

        [Test]
        public void FR8_PlayerHealth_StartsAt100_AndReachingZeroTriggersDeath()
        {
            Assert.AreEqual(100, _health.CurrentHealth);
            Assert.AreEqual(100, _health.MaxHealth);
            Assert.IsFalse(_health.IsDead);

            // Take damage
            _health.TakeDamage(40, Vector3.zero, Vector3.up, false);
            Assert.AreEqual(60, _health.CurrentHealth);
            Assert.IsFalse(_health.IsDead);

            // Lethal damage
            _health.TakeDamage(60, Vector3.zero, Vector3.up, false);
            Assert.AreEqual(0, _health.CurrentHealth);
            Assert.IsTrue(_health.IsDead);

            // Reset
            _health.ResetState();
            Assert.AreEqual(100, _health.CurrentHealth);
            Assert.IsFalse(_health.IsDead);
        }
    }
}
