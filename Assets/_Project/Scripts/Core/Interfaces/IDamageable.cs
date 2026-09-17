using UnityEngine;

namespace NullProtocol.Core
{
    public interface IDamageable
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitNormal, bool isHeadshot);
    }
}
