using System;
using UnityEngine;

namespace NullProtocol.Core
{
    [CreateAssetMenu(fileName = "PlayerStateEventChannel", menuName = "NullProtocol/Events/Player State Event Channel")]
    public class PlayerStateEventChannelSO : ScriptableObject
    {
        public event Action<int, int> OnHealthChanged; // current, max
        public event Action OnPlayerDeath;
        public event Action<bool> OnCrouchChanged; // isCrouching
        public event Action<bool> OnSprintChanged; // isSprinting
        public event Action<float> OnLeanChanged; // leanValue (-1 to 1)

        public void RaiseHealthChanged(int current, int max) => OnHealthChanged?.Invoke(current, max);
        public void RaisePlayerDeath() => OnPlayerDeath?.Invoke();
        public void RaiseCrouchChanged(bool isCrouching) => OnCrouchChanged?.Invoke(isCrouching);
        public void RaiseSprintChanged(bool isSprinting) => OnSprintChanged?.Invoke(isSprinting);
        public void RaiseLeanChanged(float leanValue) => OnLeanChanged?.Invoke(leanValue);
    }
}
