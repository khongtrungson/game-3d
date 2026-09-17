using System;
using UnityEngine;

namespace NullProtocol.Core
{
    [CreateAssetMenu(fileName = "VoidEventChannel", menuName = "NullProtocol/Events/Void Event Channel")]
    public class VoidEventChannelSO : ScriptableObject
    {
        public event Action OnEventRaised;

        public void RaiseEvent()
        {
            OnEventRaised?.Invoke();
        }
    }
}
