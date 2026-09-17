using UnityEngine;

namespace NullProtocol.Combat
{
    public struct HitscanImpact
    {
        public bool DidHit;
        public Vector3 Point;
        public Vector3 Normal;
        public Collider Collider;
        public bool IsHeadshot;
        public int DamageDealt;
        public bool Disintegrated;
        public bool Penetrated;
    }

    public static class BallisticsCalculator
    {
        public const float BarricadeDamageReduction = 0.25f; // 25% damage penalty through barricade

        public static int CalculateDamage(int baseDamage, bool isHeadshot, int headshotDamage, bool penetratesBarricade)
        {
            int raw = isHeadshot ? headshotDamage : baseDamage;
            if (penetratesBarricade)
            {
                raw = Mathf.RoundToInt(raw * (1f - BarricadeDamageReduction));
            }
            return Mathf.Max(1, raw);
        }

        public static bool IsHeadshotCollider(Collider col)
        {
            if (col == null) return false;
            if (col.CompareTag(NullProtocol.Core.Tags.EnemyHead)) return true;
            string nameLower = col.gameObject.name.ToLowerInvariant();
            return nameLower.Contains("head") || nameLower.Contains("skull");
        }

        public static bool IsBarricadeCollider(Collider col)
        {
            if (col == null) return false;
            if (col.CompareTag(NullProtocol.Core.Tags.DeployableBarricade)) return true;
            string nameLower = col.gameObject.name.ToLowerInvariant();
            return nameLower.Contains("barricade");
        }

        public static bool IsWallCollider(Collider col)
        {
            if (col == null) return false;
            if (col.CompareTag(NullProtocol.Core.Tags.GridWall)) return true;
            string nameLower = col.gameObject.name.ToLowerInvariant();
            return nameLower.Contains("wall");
        }
    }
}
