using System;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.World;
using NullProtocol.Combat;

namespace NullProtocol.UI
{
    /// <summary>
    /// Tracks combat performance metrics during a level session to generate the Tactical Debrief screen (FR-33).
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelStatisticsTracker : MonoBehaviour, IResettable
    {
        public static LevelStatisticsTracker Instance { get; private set; }

        [Header("Subsector Configuration")]
        [SerializeField] private SubsectorId _subsectorId = SubsectorId.Subsector01;
        [SerializeField] private string _subsectorTitle = "Subsector 01";
        [SerializeField] private float _parTimeSeconds = 90.0f;

        [Header("Event Channels (Optional Subscriptions)")]
        [SerializeField] private DamageEventChannelSO _damageChannel;
        [SerializeField] private KillEventChannelSO _killChannel;
        [SerializeField] private PlayerStateEventChannelSO _playerStateEvents;

        [Header("Extraction Gateway Hook")]
        [SerializeField] private ExtractionGateway _extractionGateway;

        private float _elapsedTime;
        private bool _isTracking;
        private int _shotsFired;
        private int _shotsHit;
        private int _damageTaken;
        private int _enemiesEliminated;
        private int _headshotsCount;
        private int _lastPlayerHealth = 100;

        public SubsectorId Subsector => _subsectorId;
        public string SubsectorTitle => _subsectorTitle;
        public float ElapsedTime => _elapsedTime;
        public int ShotsFired => _shotsFired;
        public int ShotsHit => _shotsHit;
        public int HeadshotsCount => _headshotsCount;
        public int DamageTaken => _damageTaken;
        public int EnemiesEliminated => _enemiesEliminated;

        public float AccuracyPercentage => (_shotsFired > 0) ? ((float)_shotsHit / _shotsFired * 100f) : 100f;
        public float HeadshotPercentage => (_shotsHit > 0) ? ((float)_headshotsCount / _shotsHit * 100f) : 0f;

        public event Action<TacticalDebriefData> OnDebriefReady;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            StartTracking();
        }

        private void OnEnable()
        {
            if (_damageChannel != null)
            {
                _damageChannel.OnEventRaised += HandleDamageDealt;
            }

            if (_killChannel != null)
            {
                _killChannel.OnKillRegistered += HandleKill;
            }

            if (_playerStateEvents != null)
            {
                _playerStateEvents.OnHealthChanged += HandlePlayerHealthChanged;
            }

            if (_extractionGateway != null)
            {
                _extractionGateway.OnExtractionCompleted += HandleExtraction;
            }
        }

        private void OnDisable()
        {
            if (_damageChannel != null)
            {
                _damageChannel.OnEventRaised -= HandleDamageDealt;
            }

            if (_killChannel != null)
            {
                _killChannel.OnKillRegistered -= HandleKill;
            }

            if (_playerStateEvents != null)
            {
                _playerStateEvents.OnHealthChanged -= HandlePlayerHealthChanged;
            }

            if (_extractionGateway != null)
            {
                _extractionGateway.OnExtractionCompleted -= HandleExtraction;
            }
        }

        private void Update()
        {
            if (_isTracking)
            {
                _elapsedTime += Time.deltaTime;
            }
        }

        public void StartTracking()
        {
            _isTracking = true;
            _elapsedTime = 0f;
            _shotsFired = 0;
            _shotsHit = 0;
            _headshotsCount = 0;
            _damageTaken = 0;
            _enemiesEliminated = 0;
            _lastPlayerHealth = 100;
        }

        public void StopTracking()
        {
            _isTracking = false;
        }

        public void RegisterShotFired()
        {
            _shotsFired++;
        }

        public void RegisterShotHit()
        {
            _shotsHit++;
        }

        public void RegisterHeadshot()
        {
            _headshotsCount++;
        }

        public void RegisterDamageTaken(int amount)
        {
            if (amount > 0)
            {
                _damageTaken += amount;
            }
        }

        private void HandleDamageDealt(int damage, Vector3 hitPoint, bool isHeadshot)
        {
            // Damage dealt to a hostile constitutes a hit
            RegisterShotHit();
            if (isHeadshot)
            {
                RegisterHeadshot();
            }
        }

        private void HandleKill(IDamageable victim, bool isHeadshot, Vector3 hitPoint)
        {
            _enemiesEliminated++;
        }

        private void HandlePlayerHealthChanged(int currentHp, int maxHp)
        {
            if (currentHp < _lastPlayerHealth)
            {
                int delta = _lastPlayerHealth - currentHp;
                RegisterDamageTaken(delta);
            }
            _lastPlayerHealth = currentHp;
        }

        private void HandleExtraction(ExtractionGateway gateway)
        {
            StopTracking();
            var debriefData = GenerateDebriefData();
            NullLog.Info("Debrief", $"Level complete! Grade: {debriefData.Grade}, Time: {debriefData.FormattedTime}, Accuracy: {debriefData.FormattedAccuracy}, Damage: {debriefData.FormattedDamage}");
            OnDebriefReady?.Invoke(debriefData);

            if (CampaignFlowManager.Instance != null)
            {
                CampaignFlowManager.Instance.CompleteCurrentSubsector();
            }
        }

        /// <summary>
        /// Generates the complete Tactical Debrief data model evaluated with tactical grade (FR-33).
        /// </summary>
        public TacticalDebriefData GenerateDebriefData()
        {
            TacticalGrade grade = TacticalGradeCalculator.CalculateGrade(
                _elapsedTime,
                AccuracyPercentage,
                _damageTaken,
                _parTimeSeconds
            );

            return new TacticalDebriefData(
                _subsectorId,
                _subsectorTitle,
                _elapsedTime,
                _shotsFired,
                _shotsHit,
                _damageTaken,
                grade,
                enemiesNeutralized: _enemiesEliminated,
                headshots: _headshotsCount
            );
        }

        public void ResetState()
        {
            StartTracking();
        }
    }
}
