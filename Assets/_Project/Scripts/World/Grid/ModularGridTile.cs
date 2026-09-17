using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.World
{
    public enum ModularTileType
    {
        Floor,
        Wall,
        Doorway,
        CornerPillar,
        CatwalkGantry,
        CoverProp,
        Ceiling
    }

    /// <summary>
    /// Component representing a modular asset conforming to the 4-meter grid architecture (FR-28).
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class ModularGridTile : MonoBehaviour
    {
        [Header("Modular Properties (FR-28)")]
        [SerializeField] private ModularTileType _tileType = ModularTileType.Floor;
        [SerializeField] private Vector2Int _gridDimensions = new Vector2Int(1, 1); // 1x1 = 4m x 4m
        [SerializeField] private bool _autoSnapOnValidate = true;

        public ModularTileType TileType => _tileType;
        public Vector2Int GridDimensions => _gridDimensions;
        public float WorldWidth => _gridDimensions.x * ModularGrid.GRID_SIZE;
        public float WorldLength => _gridDimensions.y * ModularGrid.GRID_SIZE;

        private void OnValidate()
        {
            if (_autoSnapOnValidate && !Application.isPlaying)
            {
                SnapToModularGrid();
            }
        }

        /// <summary>
        /// Snaps this tile's transform position to the 4m modular grid.
        /// </summary>
        [ContextMenu("Snap To Modular Grid")]
        public void SnapToModularGrid()
        {
            Vector3 current = transform.position;
            transform.position = ModularGrid.Snap(current);
        }

        /// <summary>
        /// Returns true if this tile strictly adheres to the 4-meter modular grid alignment.
        /// </summary>
        public bool IsGridAligned(float tolerance = 0.05f)
        {
            return ModularGrid.IsAligned(transform.position, tolerance);
        }
    }
}
