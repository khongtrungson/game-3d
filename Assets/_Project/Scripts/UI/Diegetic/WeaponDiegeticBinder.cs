using UnityEngine;
using NullProtocol.Combat;

namespace NullProtocol.UI
{
    /// <summary>
    /// Bridges BaseWeapon events to WeaponReceiverDisplay (FR-34).
    /// Listens to ammo changes, firing mode toggles, and reload states, updating the receiver mesh.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BaseWeapon))]
    public class WeaponDiegeticBinder : MonoBehaviour
    {
        [SerializeField] private BaseWeapon _weapon;
        [SerializeField] private WeaponReceiverDisplay _display;

        private void Awake()
        {
            if (_weapon == null) _weapon = GetComponent<BaseWeapon>();
            if (_display == null) _display = GetComponentInChildren<WeaponReceiverDisplay>();
        }

        private void OnEnable()
        {
            if (_weapon != null)
            {
                _weapon.OnAmmoChanged += HandleAmmoChanged;
                _weapon.OnReloadStarted += HandleReloadStarted;
                _weapon.OnReloadFinished += HandleReloadFinished;
            }

            SyncInitialState();
        }

        private void OnDisable()
        {
            if (_weapon != null)
            {
                _weapon.OnAmmoChanged -= HandleAmmoChanged;
                _weapon.OnReloadStarted -= HandleReloadStarted;
                _weapon.OnReloadFinished -= HandleReloadFinished;
            }
        }

        public void SyncInitialState()
        {
            if (_weapon == null || _display == null) return;

            int maxAmmo = (_weapon.Data != null) ? _weapon.Data.MagazineCapacity : 12;
            _display.SetAmmo(_weapon.CurrentMagazine, maxAmmo);
            _display.SetReloading(_weapon.IsReloading);
            UpdateFireMode();
        }

        private void Update()
        {
            // Update fire mode in case of runtime toggles
            UpdateFireMode();
        }

        private void UpdateFireMode()
        {
            if (_weapon == null || _display == null) return;

            int modeIndex = 0;
            if (_weapon is SynapseAR synapseAR)
            {
                modeIndex = (synapseAR.CurrentFireMode == FireMode.Burst) ? 1 : 0;
            }
            else if (_weapon is PhaseRail)
            {
                modeIndex = 2;
            }
            else if (_weapon.Data != null)
            {
                modeIndex = (int)_weapon.Data.DefaultFireMode;
            }

            if (_display.FireMode != modeIndex)
            {
                _display.SetFireMode(modeIndex);
            }
        }

        private void HandleAmmoChanged(int currentMag, int spare)
        {
            if (_display == null) return;
            int maxAmmo = (_weapon != null && _weapon.Data != null) ? _weapon.Data.MagazineCapacity : 12;
            _display.SetAmmo(currentMag, maxAmmo);
        }

        private void HandleReloadStarted()
        {
            _display?.SetReloading(true);
        }

        private void HandleReloadFinished()
        {
            _display?.SetReloading(false);
            if (_weapon != null && _weapon.Data != null)
            {
                _display?.SetAmmo(_weapon.CurrentMagazine, _weapon.Data.MagazineCapacity);
            }
        }
    }
}
