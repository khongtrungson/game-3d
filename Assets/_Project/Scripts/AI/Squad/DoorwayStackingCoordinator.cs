using System;
using System.Collections.Generic;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.AI
{
    public enum StackingSide
    {
        Left,
        Right
    }

    /// <summary>
    /// Doorway Stacking Protocol Coordinator (FR-24).
    /// Manages squad doorway stacking at thresholds of 4m door openings.
    /// Sentinels take positions on the Left and Right of the door frame,
    /// report readiness, and execute synchronized cross-breaches into the room.
    /// </summary>
    [SelectionBase]
    public class DoorwayStackingCoordinator : MonoBehaviour
    {
        [Header("Doorway Frame Offsets (4m Grid)")]
        [Tooltip("Distance left of doorway center for Left Stacker (meters)")]
        [SerializeField] private float _stackOffsetLeft = 1.2f;

        [Tooltip("Distance right of doorway center for Right Stacker (meters)")]
        [SerializeField] private float _stackOffsetRight = 1.2f;

        [Tooltip("Standback distance behind door plane (meters)")]
        [SerializeField] private float _standbackOffset = 1.0f;

        [Tooltip("Breach interior entry depth into room (meters)")]
        [SerializeField] private float _breachDepth = 3.0f;

        [Tooltip("Arrival distance tolerance to consider stacked (meters)")]
        [SerializeField] private float _stackArrivalTolerance = 0.5f;

        [Tooltip("Forward facing direction into the room to breach")]
        [SerializeField] private Vector3 _breachFacing = Vector3.forward;

        private object _leftStacker;
        private object _rightStacker;
        private bool _leftReady;
        private bool _rightReady;
        private bool _breachInitiated;

        public event Action OnStackComplete;
        public event Action OnBreachExecuted;

        public Vector3 BreachForward => transform.TransformDirection(_breachFacing).normalized;
        public Vector3 DoorCenter => transform.position;
        public bool IsBreachInitiated => _breachInitiated;
        public bool IsLeftOccupied => _leftStacker != null;
        public bool IsRightOccupied => _rightStacker != null;
        public bool IsFull => IsLeftOccupied && IsRightOccupied;
        public bool AreBothReady => _leftReady && _rightReady;

        public Vector3 GetStackPosition(StackingSide side)
        {
            Vector3 forward = BreachForward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 basePos = transform.position - forward * _standbackOffset;
            if (side == StackingSide.Left)
            {
                return basePos - right * _stackOffsetLeft;
            }
            else
            {
                return basePos + right * _stackOffsetRight;
            }
        }

        public Vector3 GetBreachDestination(StackingSide side)
        {
            Vector3 forward = BreachForward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            // Cross-breach tactic: Left stacker sweeps right into room; Right stacker sweeps left
            Vector3 enterPos = transform.position + forward * _breachDepth;
            if (side == StackingSide.Left)
            {
                return enterPos + right * 1.5f;
            }
            else
            {
                return enterPos - right * 1.5f;
            }
        }

        public bool TryAssignStacker(object sentinel, out StackingSide assignedSide)
        {
            if (_leftStacker == sentinel)
            {
                assignedSide = StackingSide.Left;
                return true;
            }
            if (_rightStacker == sentinel)
            {
                assignedSide = StackingSide.Right;
                return true;
            }

            if (_leftStacker == null)
            {
                _leftStacker = sentinel;
                _leftReady = false;
                assignedSide = StackingSide.Left;
                return true;
            }
            if (_rightStacker == null)
            {
                _rightStacker = sentinel;
                _rightReady = false;
                assignedSide = StackingSide.Right;
                return true;
            }

            assignedSide = StackingSide.Left;
            return false;
        }

        public void ReportReady(object sentinel)
        {
            if (_leftStacker == sentinel) _leftReady = true;
            if (_rightStacker == sentinel) _rightReady = true;

            NullLog.Info("DoorStack", $"Stacker reported ready. (Left: {_leftReady}, Right: {_rightReady})");

            // If both stackers are in position or if single-man stack is forced
            if (_leftReady && (_rightReady || _rightStacker == null))
            {
                OnStackComplete?.Invoke();
            }
        }

        public void ExecuteBreach()
        {
            if (_breachInitiated) return;
            _breachInitiated = true;

            NullLog.Info("DoorStack", $"Doorway breach executed at {transform.position}!");
            OnBreachExecuted?.Invoke();
        }

        public void ReleaseStacker(object sentinel)
        {
            if (_leftStacker == sentinel)
            {
                _leftStacker = null;
                _leftReady = false;
            }
            if (_rightStacker == sentinel)
            {
                _rightStacker = null;
                _rightReady = false;
            }
        }

        public void ResetCoordinator()
        {
            _leftStacker = null;
            _rightStacker = null;
            _leftReady = false;
            _rightReady = false;
            _breachInitiated = false;
        }
    }
}
