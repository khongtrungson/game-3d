using System.Collections.Generic;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Combat
{
    public class HitscanRaycaster : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private float _maxRange = 100f;
        [SerializeField] private LayerMask _hitMask = ~0;

        public float MaxRange => _maxRange;

        public List<HitscanImpact> CastShot(
            Vector3 origin,
            Vector3 direction,
            WeaponDataSO weaponConfig,
            DamageEventChannelSO damageChannel = null,
            KillEventChannelSO killChannel = null)
        {
            var impacts = new List<HitscanImpact>();
            if (weaponConfig == null) return impacts;

            direction.Normalize();
            Vector3 currentOrigin = origin;
            Vector3 currentDir = direction;
            float remainingDistance = _maxRange;
            bool penetratedBarricadeOnce = false;
            bool penetratedWallOnce = false;

            // Maximum iterations to avoid infinite penetration loops
            for (int iteration = 0; iteration < 4; iteration++)
            {
                if (!Physics.Raycast(currentOrigin, currentDir, out RaycastHit hit, remainingDistance, _hitMask, QueryTriggerInteraction.Ignore))
                {
                    // Bullet traveled to max distance with no collision
                    impacts.Add(new HitscanImpact
                    {
                        DidHit = false,
                        Point = currentOrigin + currentDir * remainingDistance,
                        Normal = -currentDir
                    });
                    break;
                }

                Collider col = hit.collider;
                bool isHead = BallisticsCalculator.IsHeadshotCollider(col);
                bool isBarricade = BallisticsCalculator.IsBarricadeCollider(col);
                bool isWall = BallisticsCalculator.IsWallCollider(col);

                int damage = BallisticsCalculator.CalculateDamage(
                    weaponConfig.BodyDamage,
                    isHead,
                    weaponConfig.HeadDamage,
                    penetratedBarricadeOnce
                );

                var impact = new HitscanImpact
                {
                    DidHit = true,
                    Point = hit.point,
                    Normal = hit.normal,
                    Collider = col,
                    IsHeadshot = isHead,
                    DamageDealt = damage,
                    Disintegrated = isHead && weaponConfig.DisintegratesOnHeadshot,
                    Penetrated = false
                };

                // Apply damage to IDamageable target if present
                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    int hpBefore = damageable.CurrentHealth;
                    damageable.TakeDamage(damage, hit.point, hit.normal, isHead);
                    damageChannel?.RaiseEvent(damage, hit.point, isHead);

                    if (hpBefore > 0 && damageable.CurrentHealth <= 0)
                    {
                        killChannel?.RaiseKill(damageable, isHead, hit.point);
                    }
                }

                // Check Penetration Rules (FR-10, FR-11, FR-12)
                // 1. Barricade penetration: Synapse-AR only
                if (isBarricade && weaponConfig.PenetratesBarricades && !penetratedBarricadeOnce)
                {
                    penetratedBarricadeOnce = true;
                    impact.Penetrated = true;
                    impacts.Add(impact);

                    // Step through barricade collider
                    float advanceDist = 0.15f;
                    currentOrigin = hit.point + currentDir * advanceDist;
                    remainingDistance -= (hit.distance + advanceDist);
                    if (remainingDistance <= 0f) break;
                    continue;
                }

                // 2. Wall penetration: Phase-Rail only (penetrates exactly 1 solid wall)
                if (isWall && weaponConfig.PenetratesOneWall && !penetratedWallOnce)
                {
                    penetratedWallOnce = true;
                    impact.Penetrated = true;
                    impacts.Add(impact);

                    // Find exit point on the other side of wall using forward offset and back-cast
                    float wallStep = Mathf.Min(weaponConfig.MaxPenetrationThickness, 0.6f) + 0.05f;
                    Vector3 exitSamplePoint = hit.point + currentDir * wallStep;

                    if (Physics.Raycast(exitSamplePoint, -currentDir, out RaycastHit exitHit, wallStep + 0.05f, _hitMask, QueryTriggerInteraction.Ignore))
                    {
                        currentOrigin = exitHit.point + currentDir * 0.05f;
                        remainingDistance -= (hit.distance + wallStep);
                    }
                    else
                    {
                        currentOrigin = exitSamplePoint;
                        remainingDistance -= (hit.distance + wallStep);
                    }

                    if (remainingDistance <= 0f) break;
                    continue;
                }

                // If non-penetrating or reached terminal penetration, stop ray
                impacts.Add(impact);
                break;
            }

            return impacts;
        }
    }
}
