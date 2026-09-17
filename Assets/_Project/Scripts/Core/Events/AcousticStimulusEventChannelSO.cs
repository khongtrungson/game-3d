using System;
using UnityEngine;

namespace NullProtocol.Core
{
    public enum AcousticStimulusType
    {
        CrouchFootstep,
        WalkFootstep,
        SprintFootstep,
        Gunfire,
        Impact
    }

    [CreateAssetMenu(fileName = "AcousticStimulusEventChannel", menuName = "NullProtocol/Events/Acoustic Stimulus Event Channel")]
    public class AcousticStimulusEventChannelSO : ScriptableObject
    {
        public event Action<Vector3, float, AcousticStimulusType> OnStimulusEmitted;

        public void RaiseStimulus(Vector3 origin, float radius, AcousticStimulusType type)
        {
            OnStimulusEmitted?.Invoke(origin, radius, type);
        }
    }
}
