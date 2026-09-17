using System;
using UnityEngine;

namespace NullProtocol.Core
{
    public enum GadgetType
    {
        HardLightBarricade,
        NullCloudSmoke,
        LogicTripMine
    }

    [CreateAssetMenu(fileName = "GadgetEventChannel", menuName = "NullProtocol/Events/Gadget Event Channel")]
    public class GadgetEventChannelSO : ScriptableObject
    {
        public event Action<GadgetType, int> OnGadgetRefunded; // gadgetType, newChargeCount
        public event Action<GadgetType, int> OnGadgetUsed;

        public void RaiseGadgetRefunded(GadgetType type, int currentCharges)
        {
            OnGadgetRefunded?.Invoke(type, currentCharges);
        }

        public void RaiseGadgetUsed(GadgetType type, int currentCharges)
        {
            OnGadgetUsed?.Invoke(type, currentCharges);
        }
    }
}
