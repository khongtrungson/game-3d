using System;

namespace NullProtocol.Core
{
    /// <summary>
    /// Implemented by hostiles that can be frozen into an immobile wireframe state by Logic-Trip Mines (FR-21).
    /// </summary>
    public interface IFreezableHostile
    {
        bool IsFrozen { get; }
        float RemainingFreezeTime { get; }
        event Action OnFrozen;
        event Action OnUnfrozen;
        void ApplyWireframeFreeze(float duration);
        void Unfreeze();
    }
}
