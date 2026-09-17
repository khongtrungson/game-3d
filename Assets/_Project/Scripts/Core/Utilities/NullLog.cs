using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace NullProtocol.Core
{
    public static class NullLog
    {
        [Conditional("DEBUG"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string category, string message) =>
            Debug.Log($"[{category}] {message}");

        [Conditional("DEBUG"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Warn(string category, string message) =>
            Debug.LogWarning($"[{category}] {message}");

        public static void Error(string category, string message) =>
            Debug.LogError($"[{category}] {message}");
    }
}
