using UnityEngine;

namespace NullProtocol.Core
{
    public static class Layers
    {
        public static readonly int Default = LayerMask.GetMask("Default");
        public static readonly int Environment = LayerMask.GetMask("Default");
        public static readonly int Wall = LayerMask.GetMask("Default");
        public static readonly int Obstacle = LayerMask.GetMask("Default");
        
        // Convenience mask for camera clipping checks
        public static LayerMask CameraObstacles => LayerMask.GetMask("Default");

        // Mask for all hitscan targets (enemies, barricades, environment walls)
        public static LayerMask HitscanTargets => ~LayerMask.GetMask("Ignore Raycast", "TransparentFX");
    }

    public static class Tags
    {
        public const string Player = "Player";
        public const string MainCamera = "MainCamera";
        public const string Enemy = "Enemy";
        public const string EnemyHead = "EnemyHead";
        public const string DeployableBarricade = "DeployableBarricade";
        public const string GridWall = "GridWall";
    }
}

