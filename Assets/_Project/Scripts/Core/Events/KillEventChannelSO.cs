using System;
using UnityEngine;

namespace NullProtocol.Core
{
    [CreateAssetMenu(fileName = "KillEventChannel", menuName = "NullProtocol/Events/Kill Event Channel")]
    public class KillEventChannelSO : ScriptableObject
    {
        // victim, isHeadshot, hitPoint
        public event Action<IDamageable, bool, Vector3> OnKillRegistered;

        public void RaiseKill(IDamageable victim, bool isHeadshot, Vector3 hitPoint)
        {
            OnKillRegistered?.Invoke(victim, isHeadshot, hitPoint);
        }
    }
}
