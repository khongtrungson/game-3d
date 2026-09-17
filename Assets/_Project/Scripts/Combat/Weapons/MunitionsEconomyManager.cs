using System;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Combat
{
    public class MunitionsEconomyManager : MonoBehaviour, IResettable
    {
        [Header("Headshot Streak Settings (FR-15)")]
        [Tooltip("Consecutive headshots without taking damage to trigger gadget refund")]
        [SerializeField] private int _requiredHeadshotStreak = 3;

        [Header("Event Channels")]
        [SerializeField] private KillEventChannelSO _killChannel;
        [SerializeField] private DamageEventChannelSO _playerDamageChannel;
        [SerializeField] private GadgetEventChannelSO _gadgetChannel;

        // Internal State
        private int _currentHeadshotStreak = 0;
        private int _totalHeadshotKills = 0;
        private int _totalRefundsGranted = 0;

        // Current gadget charges tracked for economy
        private int _barricadeCharges = 2;
        private int _smokeCharges = 2;
        private int _tripMineCharges = 1;

        public event Action<int> OnStreakChanged; // currentStreak
        public event Action<GadgetType> OnGadgetRefundAwarded;

        public int CurrentHeadshotStreak => _currentHeadshotStreak;
        public int TotalHeadshotKills => _totalHeadshotKills;
        public int TotalRefundsGranted => _totalRefundsGranted;

        public int BarricadeCharges => _barricadeCharges;
        public int SmokeCharges => _smokeCharges;
        public int TripMineCharges => _tripMineCharges;

        private void OnEnable()
        {
            if (_killChannel != null)
            {
                _killChannel.OnKillRegistered += HandleKill;
            }

            if (_playerDamageChannel != null)
            {
                _playerDamageChannel.OnEventRaised += HandlePlayerDamage;
            }
        }

        private void OnDisable()
        {
            if (_killChannel != null)
            {
                _killChannel.OnKillRegistered -= HandleKill;
            }

            if (_playerDamageChannel != null)
            {
                _playerDamageChannel.OnEventRaised -= HandlePlayerDamage;
            }
        }

        public void HandleKill(IDamageable victim, bool isHeadshot, Vector3 hitPoint)
        {
            if (isHeadshot)
            {
                _currentHeadshotStreak++;
                _totalHeadshotKills++;
                NullLog.Info("MunitionsEconomy", $"Headshot kill recorded! Current streak: {_currentHeadshotStreak}/{_requiredHeadshotStreak}");
                OnStreakChanged?.Invoke(_currentHeadshotStreak);

                if (_currentHeadshotStreak >= _requiredHeadshotStreak)
                {
                    AwardRandomGadgetRefund();
                    _currentHeadshotStreak = 0;
                    OnStreakChanged?.Invoke(_currentHeadshotStreak);
                }
            }
            else
            {
                // Non-headshot kill resets streak
                if (_currentHeadshotStreak > 0)
                {
                    NullLog.Info("MunitionsEconomy", "Non-headshot kill registered. Headshot streak reset to 0.");
                    _currentHeadshotStreak = 0;
                    OnStreakChanged?.Invoke(_currentHeadshotStreak);
                }
            }
        }

        public void HandlePlayerDamage(int amount, Vector3 hitPoint, bool isHeadshot)
        {
            // Taking any damage resets headshot streak (FR-15)
            if (amount > 0 && _currentHeadshotStreak > 0)
            {
                NullLog.Info("MunitionsEconomy", $"Player took {amount} damage! Headshot streak reset to 0.");
                _currentHeadshotStreak = 0;
                OnStreakChanged?.Invoke(_currentHeadshotStreak);
            }
        }

        public void AwardRandomGadgetRefund()
        {
            // Pick a random gadget from the 3 tactical gadget types
            var gadgetTypes = (GadgetType[])Enum.GetValues(typeof(GadgetType));
            int randomIndex = UnityEngine.Random.Range(0, gadgetTypes.Length);
            GadgetType chosenGadget = gadgetTypes[randomIndex];

            int updatedCharges = 0;
            switch (chosenGadget)
            {
                case GadgetType.HardLightBarricade:
                    _barricadeCharges++;
                    updatedCharges = _barricadeCharges;
                    break;
                case GadgetType.NullCloudSmoke:
                    _smokeCharges++;
                    updatedCharges = _smokeCharges;
                    break;
                case GadgetType.LogicTripMine:
                    _tripMineCharges++;
                    updatedCharges = _tripMineCharges;
                    break;
            }

            _totalRefundsGranted++;
            NullLog.Info("MunitionsEconomy", $"[REWARD] 3 Consecutive Headshots! Refunded 1 charge of {chosenGadget}. New count: {updatedCharges}");
            _gadgetChannel?.RaiseGadgetRefunded(chosenGadget, updatedCharges);
            OnGadgetRefundAwarded?.Invoke(chosenGadget);
        }

        public void SetInitialCharges(int barricades, int smokes, int tripMines)
        {
            _barricadeCharges = barricades;
            _smokeCharges = smokes;
            _tripMineCharges = tripMines;
        }

        public void ResetState()
        {
            _currentHeadshotStreak = 0;
            _barricadeCharges = 2;
            _smokeCharges = 2;
            _tripMineCharges = 1;
            OnStreakChanged?.Invoke(0);
        }
    }
}
