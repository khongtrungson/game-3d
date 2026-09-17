using UnityEngine;

namespace NullProtocol.World
{
    /// <summary>
    /// Core geometry and grid specifications for Null-Protocol level design (FR-28).
    /// Enforces a strict 4-meter modular grid and calibrated room sightlines (max 12 meters).
    /// </summary>
    public static class ModularGrid
    {
        /// <summary>
        /// Strict modular grid snap unit in meters (FR-28: 4.0m).
        /// </summary>
        public const float GRID_SIZE = 4.0f;

        /// <summary>
        /// Maximum allowed room combat engagement sightline in meters (FR-28: 12.0m).
        /// </summary>
        public const float MAX_SIGHTLINE_DISTANCE = 12.0f;

        /// <summary>
        /// Snaps a 3D position to the nearest 4-meter modular grid coordinates.
        /// </summary>
        /// <param name="position">Raw world position.</param>
        /// <param name="snapY">Whether to snap vertical elevation to 4m increments as well.</param>
        /// <returns>Grid-snapped world position.</returns>
        public static Vector3 Snap(Vector3 position, bool snapY = false)
        {
            float snappedX = Mathf.Round(position.x / GRID_SIZE) * GRID_SIZE;
            float snappedZ = Mathf.Round(position.z / GRID_SIZE) * GRID_SIZE;
            float snappedY = snapY ? Mathf.Round(position.y / GRID_SIZE) * GRID_SIZE : position.y;
            return new Vector3(snappedX, snappedY, snappedZ);
        }

        /// <summary>
        /// Converts a world coordinate into discrete integer grid coordinates.
        /// </summary>
        public static Vector3Int WorldToGridCoords(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x / GRID_SIZE),
                Mathf.RoundToInt(position.y / GRID_SIZE),
                Mathf.RoundToInt(position.z / GRID_SIZE)
            );
        }

        /// <summary>
        /// Converts discrete integer grid coordinates to world center position.
        /// </summary>
        public static Vector3 GridToWorldCoords(Vector3Int gridCoord)
        {
            return new Vector3(
                gridCoord.x * GRID_SIZE,
                gridCoord.y * GRID_SIZE,
                gridCoord.z * GRID_SIZE
            );
        }

        /// <summary>
        /// Validates whether a given world coordinate aligns with the 4-meter modular grid within tolerance.
        /// </summary>
        public static bool IsAligned(Vector3 position, float tolerance = 0.05f, bool checkY = false)
        {
            float xRem = Mathf.Abs(position.x % GRID_SIZE);
            float zRem = Mathf.Abs(position.z % GRID_SIZE);

            bool xAligned = xRem <= tolerance || Mathf.Abs(xRem - GRID_SIZE) <= tolerance;
            bool zAligned = zRem <= tolerance || Mathf.Abs(zRem - GRID_SIZE) <= tolerance;

            if (!checkY)
            {
                return xAligned && zAligned;
            }

            float yRem = Mathf.Abs(position.y % GRID_SIZE);
            bool yAligned = yRem <= tolerance || Mathf.Abs(yRem - GRID_SIZE) <= tolerance;
            return xAligned && zAligned && yAligned;
        }
    }
}
