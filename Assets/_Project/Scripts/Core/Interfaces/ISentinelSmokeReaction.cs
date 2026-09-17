using UnityEngine;

namespace NullProtocol.Core
{
    /// <summary>
    /// Implemented by AI sentinels responding to Null-Cloud Smoke interference (FR-19).
    /// Sentinels caught inside or blocked by smoke cease firing and seek defensive cover.
    /// </summary>
    public interface ISentinelSmokeReaction
    {
        bool IsSuppressedBySmoke { get; }
        bool IsSeekingCover { get; }
        void OnSmokeSuppressed(Vector3 smokeCenter, float smokeRadius);
        void OnSmokeCleared();
    }
}
