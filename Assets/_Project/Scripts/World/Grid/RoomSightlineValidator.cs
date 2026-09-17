using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.World
{
    /// <summary>
    /// Validates and verifies that room combat sightlines strictly adhere to the calibrated
    /// maximum of 12 meters (FR-28).
    /// </summary>
    public static class RoomSightlineValidator
    {
        public const float MAX_ALLOWED_SIGHTLINE = ModularGrid.MAX_SIGHTLINE_DISTANCE; // 12.0m

        /// <summary>
        /// Evaluates line of sight between two combatants or points.
        /// Returns true if the sightline is calibrated (either blocked by an obstacle or distance <= 12m).
        /// Returns false if an unblocked direct combat sightline exceeds 12 meters.
        /// </summary>
        public static bool IsSightlineCalibrated(Vector3 origin, Vector3 target, LayerMask obstacleMask, out float actualDistance)
        {
            Vector3 diff = target - origin;
            actualDistance = diff.magnitude;

            // If points are within 12 meters, sightline is valid by definition
            if (actualDistance <= MAX_ALLOWED_SIGHTLINE)
            {
                return true;
            }

            // If points exceed 12 meters, geometry MUST block line of sight to maintain calibration
            Vector3 direction = diff.normalized;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, actualDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // Sightline is safely broken by an obstacle
                return true;
            }

            // Unbroken sightline exceeding 12m violates FR-28
            NullLog.Warn("LevelDesign", $"FR-28 Sightline Violation: Unblocked distance {actualDistance:F1}m exceeds max 12.0m.");
            return false;
        }

        /// <summary>
        /// Probes a direction from an origin point to ensure the room geometry closes the angle within 12 meters.
        /// Returns true if an obstacle is encountered within 12.0 meters.
        /// </summary>
        public static bool IsSightlineContainedWithinDistance(Vector3 origin, Vector3 direction, LayerMask obstacleMask, float maxDistance = MAX_ALLOWED_SIGHTLINE)
        {
            direction.Normalize();
            return Physics.Raycast(origin, direction, maxDistance, obstacleMask, QueryTriggerInteraction.Ignore);
        }
    }
}
