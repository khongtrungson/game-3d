using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.World
{
    /// <summary>
    /// Level 3 Root Core arena controller (FR-31).
    /// Executes timed perimeter tile de-referencing across 3 waves,
    /// dropping outer floor tiles into the void and reducing playable area by exactly 40%.
    /// </summary>
    [DisallowMultipleComponent]
    public class PerimeterTileDeReferencer : MonoBehaviour, IResettable
    {
        public const float TARGET_TOTAL_REDUCTION_PERCENTAGE = 40.0f; // 40% reduction per FR-31
        public const int TOTAL_DE_REZ_WAVES = 3; // 3 waves per FR-31

        [Header("Arena Tiles Configuration (FR-31)")]
        [SerializeField] private List<VoidFallTile> _allArenaTiles = new List<VoidFallTile>();
        [SerializeField] private List<VoidFallTile> _wave1Tiles = new List<VoidFallTile>();
        [SerializeField] private List<VoidFallTile> _wave2Tiles = new List<VoidFallTile>();
        [SerializeField] private List<VoidFallTile> _wave3Tiles = new List<VoidFallTile>();

        [Header("Timing Settings")]
        [SerializeField] private bool _autoTimedWaves = false;
        [SerializeField] private float _timeBeforeFirstWave = 20.0f;
        [SerializeField] private float _waveInterval = 25.0f;
        [SerializeField] private float _warningDuration = 4.0f;

        private int _currentWave = 0;
        private int _initialTotalCount = 0;
        private Coroutine _sequenceRoutine;

        public int CurrentWave => _currentWave;
        public int InitialTileCount => _initialTotalCount;
        public int ActiveTileCount => GetActiveTileCount();
        public float WarningDuration => _warningDuration;

        /// <summary>
        /// Percentage of initial area that has been dropped into the void (reaches 40%).
        /// </summary>
        public float AreaReductionPercentage
        {
            get
            {
                if (_initialTotalCount <= 0) return 0f;
                int dropped = _initialTotalCount - GetActiveTileCount();
                return ((float)dropped / _initialTotalCount) * 100f;
            }
        }

        /// <summary>
        /// Remaining playable area percentage (reaches 60%).
        /// </summary>
        public float RemainingAreaPercentage => 100f - AreaReductionPercentage;

        public event Action<int, float> OnWaveWarningStarted; // waveIndex, warningDuration
        public event Action<int, float> OnWaveExecuted; // waveIndex, areaReductionPct
        public event Action OnDeRezSequenceCompleted;

        private void Awake()
        {
            if (_allArenaTiles == null || _allArenaTiles.Count == 0)
            {
                _allArenaTiles = new List<VoidFallTile>(GetComponentsInChildren<VoidFallTile>(true));
            }

            _initialTotalCount = _allArenaTiles.Count;

            // If wave lists are empty, automatically partition tiles into 3 waves to reach 40% reduction
            if (_wave1Tiles.Count == 0 && _wave2Tiles.Count == 0 && _wave3Tiles.Count == 0 && _initialTotalCount > 0)
            {
                AutoPartitionTilesForFortyPercentReduction();
            }
        }

        private void Start()
        {
            if (_autoTimedWaves)
            {
                StartDeRezTimer();
            }
        }

        /// <summary>
        /// Configures and partitions an arbitrary set of tiles into 3 waves guaranteeing 40% total reduction.
        /// </summary>
        public void ConfigureArenaTiles(List<VoidFallTile> tiles)
        {
            _allArenaTiles = new List<VoidFallTile>(tiles);
            _initialTotalCount = _allArenaTiles.Count;
            AutoPartitionTilesForFortyPercentReduction();
        }

        /// <summary>
        /// Partitions tiles so that Wave 1, Wave 2, and Wave 3 drop a cumulative 40% of all tiles.
        /// Target: exactly 40% (rounded to nearest tile count).
        /// </summary>
        public void AutoPartitionTilesForFortyPercentReduction()
        {
            _wave1Tiles.Clear();
            _wave2Tiles.Clear();
            _wave3Tiles.Clear();

            if (_allArenaTiles == null || _allArenaTiles.Count == 0) return;

            int totalTiles = _allArenaTiles.Count;
            int totalToDrop = Mathf.RoundToInt(totalTiles * (TARGET_TOTAL_REDUCTION_PERCENTAGE / 100f)); // 40% of total

            // Distribute across 3 waves: Wave 1 (~40% of drop), Wave 2 (~30% of drop), Wave 3 (~30% of drop)
            int wave1Count = Mathf.RoundToInt(totalToDrop * 0.40f);
            int wave2Count = Mathf.RoundToInt(totalToDrop * 0.30f);
            int wave3Count = totalToDrop - (wave1Count + wave2Count);

            int index = 0;
            for (int i = 0; i < wave1Count && index < totalTiles; i++, index++)
            {
                _wave1Tiles.Add(_allArenaTiles[index]);
                _allArenaTiles[index].WaveIndex = 1;
            }
            for (int i = 0; i < wave2Count && index < totalTiles; i++, index++)
            {
                _wave2Tiles.Add(_allArenaTiles[index]);
                _allArenaTiles[index].WaveIndex = 2;
            }
            for (int i = 0; i < wave3Count && index < totalTiles; i++, index++)
            {
                _wave3Tiles.Add(_allArenaTiles[index]);
                _allArenaTiles[index].WaveIndex = 3;
            }

            NullLog.Info("RootCore", $"Root Core Tiles partitioned: Total {totalTiles}, 40% reduction target = {totalToDrop} tiles. Wave1: {wave1Count}, Wave2: {wave2Count}, Wave3: {wave3Count}");
        }

        public void StartDeRezTimer()
        {
            if (_sequenceRoutine != null) StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = StartCoroutine(TimedSequenceRoutine());
        }

        private IEnumerator TimedSequenceRoutine()
        {
            yield return new WaitForSeconds(_timeBeforeFirstWave);

            for (int wave = 1; wave <= TOTAL_DE_REZ_WAVES; wave++)
            {
                yield return StartCoroutine(ExecuteWaveRoutine(wave));

                if (wave < TOTAL_DE_REZ_WAVES)
                {
                    yield return new WaitForSeconds(_waveInterval);
                }
            }

            OnDeRezSequenceCompleted?.Invoke();
            NullLog.Info("RootCore", $"All 3 perimeter de-rez waves complete. Playable area reduced by {AreaReductionPercentage:F1}%.");
        }

        /// <summary>
        /// Triggers a specific wave (1, 2, or 3) with full warning phase.
        /// </summary>
        public void TriggerWave(int waveIndex)
        {
            if (waveIndex < 1 || waveIndex > TOTAL_DE_REZ_WAVES) return;
            StartCoroutine(ExecuteWaveRoutine(waveIndex));
        }

        /// <summary>
        /// Immediately triggers a wave without warning (useful for rapid testing).
        /// </summary>
        public void TriggerWaveImmediate(int waveIndex)
        {
            var tiles = GetTilesForWave(waveIndex);
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i]?.DropImmediately();
            }
            _currentWave = Mathf.Max(_currentWave, waveIndex);
            OnWaveExecuted?.Invoke(_currentWave, AreaReductionPercentage);
        }

        private IEnumerator ExecuteWaveRoutine(int waveIndex)
        {
            _currentWave = waveIndex;
            var tiles = GetTilesForWave(waveIndex);

            NullLog.Info("RootCore", $"Root Core: Initiating Wave {waveIndex} warning phase ({_warningDuration}s).");
            OnWaveWarningStarted?.Invoke(waveIndex, _warningDuration);

            // Trigger warning phase on all tiles in this wave
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i]?.StartWarningPhase(_warningDuration);
            }

            yield return new WaitForSeconds(_warningDuration);

            NullLog.Info("RootCore", $"Root Core: Wave {waveIndex} executed. Perimeter tiles dropped into void.");
            OnWaveExecuted?.Invoke(waveIndex, AreaReductionPercentage);
        }

        public List<VoidFallTile> GetTilesForWave(int waveIndex)
        {
            return waveIndex switch
            {
                1 => _wave1Tiles,
                2 => _wave2Tiles,
                3 => _wave3Tiles,
                _ => new List<VoidFallTile>()
            };
        }

        private int GetActiveTileCount()
        {
            int count = 0;
            for (int i = 0; i < _allArenaTiles.Count; i++)
            {
                if (_allArenaTiles[i] != null && _allArenaTiles[i].IsActiveInArena)
                {
                    count++;
                }
            }
            return count;
        }

        public void ResetState()
        {
            if (_sequenceRoutine != null)
            {
                StopCoroutine(_sequenceRoutine);
                _sequenceRoutine = null;
            }

            _currentWave = 0;
            for (int i = 0; i < _allArenaTiles.Count; i++)
            {
                _allArenaTiles[i]?.ResetState();
            }

            NullLog.Info("RootCore", "Root Core Arena fully restored. Playable area back to 100%.");
        }
    }
}
