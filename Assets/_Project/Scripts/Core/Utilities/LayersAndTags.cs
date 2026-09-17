using UnityEngine;

namespace NullProtocol.Core
{
    public static class Layers
    {
        public static readonly int Default = LayerMask.GetMask("Default");
        public static readonly int Environment = LayerMask.GetMask("Default"); // Can expand to custom layers
        public static readonly int Wall = LayerMask.GetMask("Default");
        public static readonly int Obstacle = LayerMask.GetMask("Default");
        
        // Convenience mask for camera clipping checks
        public static LayerMask CameraObstacles => LayerMask.GetMask("Default");
    }

    public static class Tags
    {
        public const string Player = "Player";
        public const string MainCamera = "MainCamera";
        public const string Enemy = "Enemy";
    }
}
