using UnityEngine;

namespace NullProtocol.Core
{
    /// <summary>
    /// Implemented by volumetric obscurants (e.g. Null-Cloud Smoke) to block AI line-of-sight raycasts (FR-19).
    /// </summary>
    public interface ILOSBlocker
    {
        bool IsActive { get; }
        Vector3 Center { get; }
        float Radius { get; }
        bool BlocksLineOfSight(Vector3 from, Vector3 to);
        bool IsPointInside(Vector3 point);
    }
}
