using System;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.AI
{
    public enum CoverHeight
    {
        Low,  // Crouching cover (~1.1m, e.g. Hard-Light Barricade, crates)
        High  // Standing cover (~2.0m+, e.g. pillars, doorway frames, walls)
    }

    /// <summary>
    /// Ambient Cover Node (FR-23).
    /// Placed strategically around the 4m modular grid environment.
    /// Provides spatial metrics for peeking (left, right, over) and crouching protection.
    /// </summary>
    [SelectionBase]
    public class CoverNode : MonoBehaviour
    {
        [Header("Cover Properties")]
        [Tooltip("Height categorization of this cover obstacle")]
        [SerializeField] private CoverHeight _coverHeight = CoverHeight.Low;

        [Tooltip("Outward normal of the cover obstacle facing the danger zone")]
        [SerializeField] private Vector3 _coverNormal = Vector3.forward;

        [Tooltip("Horizontal peek clearance distance (meters)")]
        [SerializeField] private float _peekHorizontalOffset = 0.6f;

        [Tooltip("Vertical peek clearance distance for low cover (meters)")]
        [SerializeField] private float _peekVerticalOffset = 0.5f;

        [Tooltip("Allows peeking to the left")]
        [SerializeField] private bool _canPeekLeft = true;

        [Tooltip("Allows peeking to the right")]
        [SerializeField] private bool _canPeekRight = true;

        [Tooltip("Allows peeking over top (valid for Low cover)")]
        [SerializeField] private bool _canPeekOver = true;

        private bool _isOccupied;
        private object _occupant;

        public CoverHeight CoverHeight => _coverHeight;
        public Vector3 CoverNormal => transform.TransformDirection(_coverNormal).normalized;
        public Vector3 Position => transform.position;
        public bool IsOccupied => _isOccupied;
        public object Occupant => _occupant;
        public float PeekHorizontalOffset => _peekHorizontalOffset;
        public float PeekVerticalOffset => _peekVerticalOffset;
        public bool CanPeekLeft => _canPeekLeft;
        public bool CanPeekRight => _canPeekRight;
        public bool CanPeekOver => _canPeekOver && _coverHeight == CoverHeight.Low;

        private void OnEnable()
        {
            CoverNodeRegistry.Register(this);
        }

        private void OnDisable()
        {
            CoverNodeRegistry.Unregister(this);
            ReleaseOccupancy();
        }

        public bool ClaimOccupancy(object occupant)
        {
            if (_isOccupied && _occupant != occupant)
                return false;

            _isOccupied = true;
            _occupant = occupant;
            return true;
        }

        public void ReleaseOccupancy(object occupant = null)
        {
            if (occupant == null || _occupant == occupant)
            {
                _isOccupied = false;
                _occupant = null;
            }
        }

        /// <summary>
        /// Computes a quality score for protecting against a threat at threatPos.
        /// Higher score means better cover.
        /// </summary>
        public float EvaluateCoverQuality(Vector3 threatPos, Vector3 sentinelPos)
        {
            Vector3 toThreat = (threatPos - transform.position).normalized;
            Vector3 normal = CoverNormal;

            // Dot product between cover facing and vector towards threat.
            // If normal points towards threat, dot product > 0.
            float dotFacing = Vector3.Dot(normal, toThreat);
            if (dotFacing < 0.1f)
            {
                // Threat is flanking behind cover
                return -100f;
            }

            float distToSentinel = Vector3.Distance(transform.position, sentinelPos);
            float distToThreat = Vector3.Distance(transform.position, threatPos);

            // Closer to sentinel is preferred; facing directly against threat is preferred
            float score = (dotFacing * 50f) - (distToSentinel * 2f) + Mathf.Min(distToThreat, 20f);

            // Favor high cover slightly for total concealment
            if (_coverHeight == CoverHeight.High)
                score += 5f;

            return score;
        }

        /// <summary>
        /// Calculates the world-space peek position for left, right, or over.
        /// </summary>
        public Vector3 GetPeekPosition(PeekDirection direction)
        {
            Vector3 right = Vector3.Cross(Vector3.up, CoverNormal).normalized;
            switch (direction)
            {
                case PeekDirection.Left:
                    return transform.position - right * _peekHorizontalOffset;
                case PeekDirection.Right:
                    return transform.position + right * _peekHorizontalOffset;
                case PeekDirection.Over:
                    return transform.position + Vector3.up * _peekVerticalOffset;
                default:
                    return transform.position;
            }
        }

        /// <summary>
        /// Calculates the crouching concealed stance position behind the cover node.
        /// </summary>
        public Vector3 GetCrouchConcealedPosition()
        {
            // Position slightly behind the cover obstacle normal
            return transform.position - CoverNormal * 0.4f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isOccupied ? Color.red : Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, _coverHeight == CoverHeight.High ? 2.0f : 1.1f, 0.5f));
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, CoverNormal * 1.0f);
        }
#endif
    }

    public enum PeekDirection
    {
        None,
        Left,
        Right,
        Over
    }
}
