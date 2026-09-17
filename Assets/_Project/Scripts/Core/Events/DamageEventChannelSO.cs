using System;
using UnityEngine;

namespace NullProtocol.Core
{
    [CreateAssetMenu(fileName = "DamageEventChannel", menuName = "NullProtocol/Events/Damage Event Channel")]
    public class DamageEventChannelSO : ScriptableObject
    {
        public event Action<int, Vector3, bool> OnEventRaised;

        public void RaiseEvent(int damageAmount, Vector3 hitPoint, bool isHeadshot)
        {
            OnEventRaised?.Invoke(damageAmount, hitPoint, isHeadshot);
        }
    }
}
