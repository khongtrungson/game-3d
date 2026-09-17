using System;
using System.Collections;
using UnityEngine;

namespace NullProtocol.Combat
{
    public class PhaseRail : BaseWeapon
    {
        private bool _isCharging;
        private float _chargeStartTime;

        public event Action<float> OnChargeProgress; // 0 to 1
        public event Action OnChargeCompleted;
        public event Action OnChargeCanceled;

        public bool IsCharging => _isCharging;
        public float CurrentChargeProgress => _isCharging ? Mathf.Clamp01((Time.time - _chargeStartTime) / _data.ChargeTime) : 0f;

        public override bool CanFire()
        {
            if (_isCharging) return false;
            return base.CanFire();
        }

        public void StartCharging(Vector3 shootOrigin, Vector3 shootDirection, float recoilModifier = 1.0f)
        {
            if (!CanFire()) return;

            StartCoroutine(ChargeAndFireRoutine(shootOrigin, shootDirection, recoilModifier));
        }

        public void CancelCharge()
        {
            if (!_isCharging) return;
            StopAllCoroutines();
            _isCharging = false;
            OnChargeCanceled?.Invoke();
        }

        public override void PrimaryFire(Vector3 shootOrigin, Vector3 shootDirection, float recoilModifier = 1.0f)
        {
            StartCharging(shootOrigin, shootDirection, recoilModifier);
        }

        private IEnumerator ChargeAndFireRoutine(Vector3 shootOrigin, Vector3 shootDirection, float recoilModifier)
        {
            _isCharging = true;
            _chargeStartTime = Time.time;
            float chargeDuration = _data.ChargeTime;

            while (Time.time - _chargeStartTime < chargeDuration)
            {
                float progress = (Time.time - _chargeStartTime) / chargeDuration;
                OnChargeProgress?.Invoke(progress);
                yield return null;
            }

            _isCharging = false;
            OnChargeCompleted?.Invoke();

            ExecuteRaycastAndEffects(shootOrigin, shootDirection, recoilModifier);
        }

        public override void ResetState()
        {
            _isCharging = false;
            base.ResetState();
        }
    }
}
