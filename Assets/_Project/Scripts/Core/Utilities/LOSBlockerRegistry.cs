using System.Collections.Generic;
using UnityEngine;

namespace NullProtocol.Core
{
    /// <summary>
    /// Global registry for active volumetric line-of-sight blockers (FR-19).
    /// Used by AI perception systems to evaluate whether sightlines are occluded by digital static smoke.
    /// </summary>
    public static class LOSBlockerRegistry
    {
        private static readonly List<ILOSBlocker> s_ActiveBlockers = new List<ILOSBlocker>();

        public static IReadOnlyList<ILOSBlocker> ActiveBlockers => s_ActiveBlockers;

        public static void Register(ILOSBlocker blocker)
        {
            if (blocker != null && !s_ActiveBlockers.Contains(blocker))
            {
                s_ActiveBlockers.Add(blocker);
            }
        }

        public static void Unregister(ILOSBlocker blocker)
        {
            if (blocker != null)
            {
                s_ActiveBlockers.Remove(blocker);
            }
        }

        public static void Clear()
        {
            s_ActiveBlockers.Clear();
        }

        public static bool IsLOSBlocked(Vector3 from, Vector3 to)
        {
            for (int i = 0; i < s_ActiveBlockers.Count; i++)
            {
                var blocker = s_ActiveBlockers[i];
                if (blocker != null && blocker.IsActive && blocker.BlocksLineOfSight(from, to))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsPointInSmoke(Vector3 point)
        {
            for (int i = 0; i < s_ActiveBlockers.Count; i++)
            {
                var blocker = s_ActiveBlockers[i];
                if (blocker != null && blocker.IsActive && blocker.IsPointInside(point))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
