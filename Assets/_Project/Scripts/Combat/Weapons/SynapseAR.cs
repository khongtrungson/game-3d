using System.Collections;
using UnityEngine;

namespace NullProtocol.Combat
{
    public class SynapseAR : BaseWeapon
    {
        [Header("Fire Mode Selection")]
        [SerializeField] private FireMode _currentFireMode = FireMode.Burst;

        private bool _isBursting;

        public FireMode CurrentFireMode => _currentFireMode;
        public bool IsBursting => _isBursting;

        public void ToggleFireMode()
        {
            if (_isBursting || _isReloading) return;

            _currentFireMode = (_currentFireMode == FireMode.SemiAuto) ? FireMode.Burst : FireMode.SemiAuto;
            NullProtocol.Core.NullLog.Info("Combat", $"Synapse-AR switched fire mode to: {_currentFireMode}");
        }

        public void SetFireMode(FireMode mode)
        {
            _currentFireMode = mode;
        }

        public override bool CanFire()
        {
            if (_isBursting) return false;
            return base.CanFire();
        }

        public override void PrimaryFire(Vector3 shootOrigin, Vector3 shootDirection, float recoilModifier = 1.0f)
        {
            if (!CanFire()) return;

            if (_currentFireMode == FireMode.SemiAuto)
            {
                ExecuteRaycastAndEffects(shootOrigin, shootDirection, recoilModifier);
            }
            else if (_currentFireMode == FireMode.Burst)
            {
                StartCoroutine(BurstRoutine(shootOrigin, shootDirection, recoilModifier));
            }
        }

        private IEnumerator BurstRoutine(Vector3 shootOrigin, Vector3 shootDirection, float recoilModifier)
        {
            _isBursting = true;
            int shotsToFire = Mathf.Min(_data.BurstCount, _currentMagazine);

            for (int i = 0; i < shotsToFire; i++)
            {
                ExecuteRaycastAndEffects(shootOrigin, shootDirection, recoilModifier);

                if (i < shotsToFire - 1)
                {
                    yield return new WaitForSeconds(_data.BurstInterval);
                }
            }

            _isBursting = false;
        }

        public override void ResetState()
        {
            _isBursting = false;
            base.ResetState();
        }
    }
}
