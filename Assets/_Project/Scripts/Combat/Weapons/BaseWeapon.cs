using System;
using System.Collections;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Combat
{
    public abstract class BaseWeapon : MonoBehaviour, IResettable
    {
        [Header("Weapon Data Configuration")]
        [SerializeField] protected WeaponDataSO _data;

        [Header("Muzzle Transform")]
        [SerializeField] protected Transform _muzzlePoint;

        [Header("Event Channels")]
        [SerializeField] protected DamageEventChannelSO _damageChannel;
        [SerializeField] protected KillEventChannelSO _killChannel;
        [SerializeField] protected AcousticStimulusEventChannelSO _acousticChannel;

        [Header("Components")]
        [SerializeField] protected HitscanRaycaster _raycaster;
        [SerializeField] protected WeaponVFXController _vfx;
        [SerializeField] protected ProceduralRecoilSpring _recoilSpring;

        // Runtime Ammo State
        protected int _currentMagazine;
        protected int _spareAmmo;
        protected bool _isReloading;
        protected float _lastFireTime;
        protected float _reloadEndTime;

        // Events for diegetic UI binding
        public event Action<int, int> OnAmmoChanged; // currentMag, spareAmmo
        public event Action OnReloadStarted;
        public event Action OnReloadFinished;
        public event Action OnWeaponFired;

        public WeaponDataSO Data => _data;
        public int CurrentMagazine => _currentMagazine;
        public int SpareAmmo => _spareAmmo;
        public bool IsReloading => _isReloading;
        public ProceduralRecoilSpring RecoilSpring => _recoilSpring;

        protected virtual void Awake()
        {
            if (_data != null)
            {
                _currentMagazine = _data.MagazineCapacity;
                _spareAmmo = _data.InitialSpareAmmo;
            }

            if (_raycaster == null)
            {
                _raycaster = GetComponent<HitscanRaycaster>() ?? gameObject.AddComponent<HitscanRaycaster>();
            }

            if (_vfx == null)
            {
                _vfx = GetComponent<WeaponVFXController>() ?? gameObject.AddComponent<WeaponVFXController>();
            }

            if (_recoilSpring == null)
            {
                _recoilSpring = GetComponent<ProceduralRecoilSpring>() ?? gameObject.AddComponent<ProceduralRecoilSpring>();
            }
        }

        protected virtual void Start()
        {
            NotifyAmmoChanged();
        }

        public virtual bool CanFire()
        {
            if (_isReloading) return false;
            if (_currentMagazine <= 0) return false;
            if (Time.time < _lastFireTime + _data.FireInterval) return false;
            return true;
        }

        public abstract void PrimaryFire(Vector3 shootOrigin, Vector3 shootDirection, float recoilModifier = 1.0f);

        public virtual bool CanReload()
        {
            if (_isReloading) return false;
            if (_currentMagazine >= _data.MagazineCapacity) return false;
            if (_spareAmmo <= 0) return false;
            return true;
        }

        public virtual void Reload()
        {
            if (!CanReload()) return;

            StartCoroutine(ReloadRoutine());
        }

        protected virtual IEnumerator ReloadRoutine()
        {
            _isReloading = true;
            _reloadEndTime = Time.time + _data.ReloadTime;
            OnReloadStarted?.Invoke();
            NullLog.Info("Combat", $"{_data.WeaponName} reloading... duration: {_data.ReloadTime}s");

            yield return new WaitForSeconds(_data.ReloadTime);

            int needed = _data.MagazineCapacity - _currentMagazine;
            int amountToLoad = Mathf.Min(needed, _spareAmmo);
            _currentMagazine += amountToLoad;
            _spareAmmo -= amountToLoad;

            _isReloading = false;
            OnReloadFinished?.Invoke();
            NotifyAmmoChanged();
            NullLog.Info("Combat", $"{_data.WeaponName} reload complete. Mag: {_currentMagazine}, Spare: {_spareAmmo}");
        }

        protected virtual void ExecuteRaycastAndEffects(Vector3 shootOrigin, Vector3 shootDirection, float recoilModifier)
        {
            _currentMagazine--;
            _lastFireTime = Time.time;
            OnWeaponFired?.Invoke();
            NotifyAmmoChanged();

            // Calculate recoil kick with recoil modifier (e.g. 0.7 when crouched)
            float pitchKick = _data.RecoilPitchAngle * recoilModifier;
            float yawSpread = _data.RecoilYawSpread * recoilModifier;
            float shockwave = (_data.WeaponId == WeaponId.PhaseRail) ? 6.0f * recoilModifier : 0f;
            _recoilSpring.ApplyRecoilKick(pitchKick, yawSpread, shockwave);

            // Raycast processing
            var impacts = _raycaster.CastShot(shootOrigin, shootDirection, _data, _damageChannel, _killChannel);

            // Audio & VFX
            Vector3 muzzlePos = (_muzzlePoint != null) ? _muzzlePoint.position : shootOrigin;
            Vector3 targetPos = (impacts.Count > 0) ? impacts[0].Point : (shootOrigin + shootDirection * _raycaster.MaxRange);
            _vfx.PlayFireEffects(muzzlePos, targetPos);

            foreach (var impact in impacts)
            {
                if (impact.DidHit)
                {
                    _vfx.PlayImpactEffects(impact.Point, impact.Normal, impact.IsHeadshot, impact.Disintegrated);
                }
            }

            // Emit acoustic footprint if not silenced
            if (!_data.IsSilenced && _acousticChannel != null)
            {
                _acousticChannel.RaiseStimulus(muzzlePos, _data.AcousticNoiseRadius, AcousticStimulusType.Gunfire);
            }
        }

        protected void NotifyAmmoChanged()
        {
            OnAmmoChanged?.Invoke(_currentMagazine, _spareAmmo);
        }

        public virtual void ResetState()
        {
            StopAllCoroutines();
            _isReloading = false;
            if (_data != null)
            {
                _currentMagazine = _data.MagazineCapacity;
                _spareAmmo = _data.InitialSpareAmmo;
            }
            if (_recoilSpring != null)
            {
                _recoilSpring.ResetSpring();
            }
            NotifyAmmoChanged();
        }
    }
}
