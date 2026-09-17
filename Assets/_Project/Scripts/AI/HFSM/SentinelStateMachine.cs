using System;
using System.Collections.Generic;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.AI
{
    public interface IState
    {
        void Enter();
        void Update();
        void FixedUpdate();
        void Exit();
    }

    /// <summary>
    /// Hierarchical Finite State Machine for Sentinel AI agents.
    /// Provides zero-allocation state transitions, updates, and telemetry.
    /// </summary>
    public class SentinelStateMachine
    {
        public IState CurrentState { get; private set; }
        public string CurrentStateName => CurrentState != null ? CurrentState.GetType().Name : "None";

        public event Action<IState, IState> OnStateChanged;

        public void ChangeState(IState newState)
        {
            if (CurrentState == newState) return;

            IState previousState = CurrentState;
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState?.Enter();

            OnStateChanged?.Invoke(previousState, newState);
        }

        public void Update()
        {
            CurrentState?.Update();
        }

        public void FixedUpdate()
        {
            CurrentState?.FixedUpdate();
        }
    }
}
