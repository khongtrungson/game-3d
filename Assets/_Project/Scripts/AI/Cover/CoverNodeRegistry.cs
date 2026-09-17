using System;
using System.Collections.Generic;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.AI
{
    /// <summary>
    /// Static registry tracking active cover nodes across levels and rooms.
    /// Eliminates expensive runtime Object.Find queries and satisfies Ryan Hipple decoupling principles.
    /// </summary>
    public static class CoverNodeRegistry
    {
        private static readonly List<CoverNode> s_ActiveNodes = new List<CoverNode>();

        public static IReadOnlyList<CoverNode> ActiveNodes => s_ActiveNodes;

        public static void Register(CoverNode node)
        {
            if (node != null && !s_ActiveNodes.Contains(node))
            {
                s_ActiveNodes.Add(node);
            }
        }

        public static void Unregister(CoverNode node)
        {
            if (node != null)
            {
                s_ActiveNodes.Remove(node);
            }
        }

        public static void Clear()
        {
            s_ActiveNodes.Clear();
        }

        /// <summary>
        /// Finds the best available cover node protecting from threatPosition within maxDistance.
        /// Evaluates dot product against threat vector, height, obstacle occlusion, and distance.
        /// </summary>
        public static CoverNode FindBestCoverNode(Vector3 sentinelPos, Vector3 threatPos, float maxDistance = 12f, CoverNode currentExcluded = null)
        {
            CoverNode bestNode = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < s_ActiveNodes.Count; i++)
            {
                var node = s_ActiveNodes[i];
                if (node == null || !node.gameObject.activeInHierarchy || node.IsOccupied)
                    continue;

                if (node == currentExcluded)
                    continue;

                float distToSentinel = Vector3.Distance(sentinelPos, node.Position);
                if (distToSentinel > maxDistance)
                    continue;

                float score = node.EvaluateCoverQuality(threatPos, sentinelPos);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestNode = node;
                }
            }

            return bestNode;
        }
    }
}
