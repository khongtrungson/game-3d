using UnityEngine;

namespace NullProtocol.Combat
{
    public enum FireMode
    {
        SemiAuto,
        Burst,
        Charge
    }

    public enum WeaponId
    {
        Vector9,
        SynapseAR,
        PhaseRail
    }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "NullProtocol/Weapons/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Identity")]
        public WeaponId WeaponId;
        public string WeaponName = "Weapon";

        [Header("Damage Profile (FR-10, FR-11, FR-12)")]
        [Tooltip("Direct damage applied to body")]
        public int BodyDamage = 30;

        [Tooltip("Damage applied on headshot")]
        public int HeadDamage = 75;

        [Tooltip("Whether headshots instantly disintegrate target")]
        public bool DisintegratesOnHeadshot = false;

        [Header("Magazine & Ammo Economy (FR-10, FR-11, FR-12, FR-15)")]
        [Tooltip("Rounds per magazine")]
        public int MagazineCapacity = 12;

        [Tooltip("Total fixed spare ammo provided at level start")]
        public int InitialSpareAmmo = 36;

        [Tooltip("Seconds required to reload / battery cycle")]
        public float ReloadTime = 1.2f;

        [Header("Firing Mechanics & Rates")]
        [Tooltip("Max Rounds Per Minute")]
        public float RoundsPerMinute = 400f;

        public FireMode DefaultFireMode = FireMode.SemiAuto;
        public bool AllowsBurst = false;
        public int BurstCount = 3;
        public float BurstRoundsPerMinute = 550f;

        [Tooltip("Charge duration before release for charge weapons (FR-12: 0.6s)")]
        public float ChargeTime = 0.6f;

        [Header("Acoustics")]
        [Tooltip("True if weapon has silent acoustic footprint (Vector-9)")]
        public bool IsSilenced = false;
        public float AcousticNoiseRadius = 25f;

        [Header("Recoil & Penetration (FR-9, FR-10, FR-11, FR-12)")]
        [Tooltip("Vertical recoil climb in degrees per shot")]
        public float RecoilPitchAngle = 0.8f;

        [Tooltip("Horizontal random recoil deviation in degrees")]
        public float RecoilYawSpread = 0.2f;

        [Tooltip("Penetrates deployable barricades with 25% damage penalty (Synapse-AR)")]
        public bool PenetratesBarricades = false;

        [Tooltip("Penetrates exactly 1 solid wireframe wall with 0% penalty (Phase-Rail)")]
        public bool PenetratesOneWall = false;

        [Tooltip("Wall penetration max thickness in meters")]
        public float MaxPenetrationThickness = 0.6f;

        [Header("ADS & Sway (FR-14)")]
        [Tooltip("ADS Field of View zoom multiplier (0.85 = 15% reduction)")]
        public float AdsFovMultiplier = 0.85f;

        [Tooltip("Procedural weapon sway damping multiplier during ADS (0.2 = 80% damping)")]
        public float AdsSwayDampening = 0.2f;

        public float FireInterval => 60f / Mathf.Max(1f, RoundsPerMinute);
        public float BurstInterval => 60f / Mathf.Max(1f, BurstRoundsPerMinute);
    }
}
