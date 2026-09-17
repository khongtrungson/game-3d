using UnityEngine;

namespace NullProtocol.Combat
{
    public class Vector9Pistol : BaseWeapon
    {
        public override void PrimaryFire(Vector3 shootOrigin, Vector3 shootDirection, float recoilModifier = 1.0f)
        {
            if (!CanFire()) return;

            ExecuteRaycastAndEffects(shootOrigin, shootDirection, recoilModifier);
        }
    }
}
