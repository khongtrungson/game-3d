using System;
using System.Collections.Generic;
using UnityEngine;
using NullProtocol.Core;
using NullProtocol.UI;
using NullProtocol.World;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Checkpoint state captured upon entering uncleared rooms and on level milestones (FR-41).
    /// </summary>
    [Serializable]
    public class CheckpointData
    {
        public bool HasActiveCheckpoint;
        public SubsectorId Subsector = SubsectorId.Subsector01;
        public string RoomId = string.Empty;
        public string RoomDisplayName = string.Empty;
        public int PlayerHealth = 100;
        public int PlayerMaxHealth = 100;
        public int BarricadeCharges = 2;
        public int SmokeCharges = 2;
        public int TripMineCharges = 1;
        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation = Quaternion.identity;
        public string Timestamp = string.Empty;

        public CheckpointData()
        {
            HasActiveCheckpoint = false;
        }

        public CheckpointData(
            SubsectorId subsector,
            string roomId,
            string roomDisplayName,
            int health,
            int maxHealth,
            int barricades,
            int smokes,
            int tripMines,
            Vector3 position,
            Quaternion rotation)
        {
            HasActiveCheckpoint = true;
            Subsector = subsector;
            RoomId = roomId;
            RoomDisplayName = roomDisplayName;
            PlayerHealth = health;
            PlayerMaxHealth = maxHealth;
            BarricadeCharges = barricades;
            SmokeCharges = smokes;
            TripMineCharges = tripMines;
            PlayerPosition = position;
            PlayerRotation = rotation;
            Timestamp = DateTime.UtcNow.ToString("o");
        }

        public void Clear()
        {
            HasActiveCheckpoint = false;
            RoomId = string.Empty;
            RoomDisplayName = string.Empty;
        }
    }

    /// <summary>
    /// Progress record and tactical debrief high scores for a specific subsector (FR-33, FR-40).
    /// </summary>
    [Serializable]
    public class SubsectorProgressRecord
    {
        public SubsectorId Subsector;
        public bool IsCompleted;
        public TacticalGrade BestGrade;
        public float BestTimeSeconds;
        public float BestAccuracyPercentage;
        public int LeastDamageTaken;
        public int CompletionsCount;

        public SubsectorProgressRecord()
        {
            Subsector = SubsectorId.Subsector01;
            IsCompleted = false;
            BestGrade = TacticalGrade.C;
            BestTimeSeconds = float.MaxValue;
            BestAccuracyPercentage = 0f;
            LeastDamageTaken = int.MaxValue;
            CompletionsCount = 0;
        }

        public SubsectorProgressRecord(SubsectorId subsector)
        {
            Subsector = subsector;
            IsCompleted = false;
            BestGrade = TacticalGrade.C;
            BestTimeSeconds = float.MaxValue;
            BestAccuracyPercentage = 0f;
            LeastDamageTaken = int.MaxValue;
            CompletionsCount = 0;
        }

        public void RecordCompletion(TacticalDebriefData debrief)
        {
            IsCompleted = true;
            CompletionsCount++;

            if (debrief.Grade < BestGrade) // TacticalGrade S (0) is better than A (1), etc.
            {
                BestGrade = debrief.Grade;
            }

            if (debrief.ElapsedTimeSeconds < BestTimeSeconds)
            {
                BestTimeSeconds = debrief.ElapsedTimeSeconds;
            }

            if (debrief.AccuracyPercentage > BestAccuracyPercentage)
            {
                BestAccuracyPercentage = debrief.AccuracyPercentage;
            }

            if (debrief.DamageTaken < LeastDamageTaken)
            {
                LeastDamageTaken = debrief.DamageTaken;
            }
        }
    }

    /// <summary>
    /// Lifetime combat metrics and retention tracking (FR-40, FR-43).
    /// </summary>
    [Serializable]
    public class LifetimeMetrics
    {
        public float TotalPlayTimeSeconds;
        public int TotalShotsFired;
        public int TotalShotsHit;
        public int TotalHeadshots;
        public int TotalDamageTaken;
        public int TotalEnemiesEliminated;
        public int TotalRoomsCleared;
        public int TotalZeroDamageRoomsCleared;
        public int TotalBarricadesDeployed;
        public int TotalSmokesDeployed;
        public int TotalTripMinesFrozen;
        public int TotalMemoryDumpResets;

        public float OverallAccuracy => (TotalShotsFired > 0) ? ((float)TotalShotsHit / TotalShotsFired * 100f) : 0f;
        public float OverallHeadshotRate => (TotalShotsHit > 0) ? ((float)TotalHeadshots / TotalShotsHit * 100f) : 0f;
    }

    /// <summary>
    /// Root data model serialized into profile.json (FR-40).
    /// </summary>
    [Serializable]
    public class ProfileData
    {
        public const int CURRENT_SCHEMA_VERSION = 1;

        public int SchemaVersion = CURRENT_SCHEMA_VERSION;
        public string ProfileId = "Agent_01";
        public string LastSavedTimestamp = string.Empty;

        // Campaign Progression (FR-29, FR-40)
        public SubsectorId CurrentSubsector = SubsectorId.Subsector01;
        public List<SubsectorId> UnlockedSubsectors = new List<SubsectorId> { SubsectorId.Subsector01 };
        public List<SubsectorProgressRecord> SubsectorRecords = new List<SubsectorProgressRecord>();

        // Checkpoint State (FR-41)
        public CheckpointData ActiveCheckpoint = new CheckpointData();

        // Steam Achievements (FR-43)
        public List<string> UnlockedAchievements = new List<string>();

        // Lifetime Metrics (FR-40)
        public LifetimeMetrics Metrics = new LifetimeMetrics();

        public ProfileData()
        {
            EnsureSubsectorRecords();
        }

        public void EnsureSubsectorRecords()
        {
            if (SubsectorRecords == null)
            {
                SubsectorRecords = new List<SubsectorProgressRecord>();
            }

            var allSubsectors = (SubsectorId[])Enum.GetValues(typeof(SubsectorId));
            foreach (var sub in allSubsectors)
            {
                if (GetRecord(sub) == null)
                {
                    SubsectorRecords.Add(new SubsectorProgressRecord(sub));
                }
            }
        }

        public SubsectorProgressRecord GetRecord(SubsectorId subsector)
        {
            if (SubsectorRecords == null) return null;
            for (int i = 0; i < SubsectorRecords.Count; i++)
            {
                if (SubsectorRecords[i].Subsector == subsector)
                {
                    return SubsectorRecords[i];
                }
            }
            return null;
        }

        public bool IsSubsectorUnlocked(SubsectorId subsector)
        {
            return UnlockedSubsectors != null && UnlockedSubsectors.Contains(subsector);
        }

        public void UnlockSubsector(SubsectorId subsector)
        {
            if (UnlockedSubsectors == null)
            {
                UnlockedSubsectors = new List<SubsectorId>();
            }

            if (!UnlockedSubsectors.Contains(subsector))
            {
                UnlockedSubsectors.Add(subsector);
            }
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            return UnlockedAchievements != null && UnlockedAchievements.Contains(achievementId);
        }

        public bool UnlockAchievement(string achievementId)
        {
            if (UnlockedAchievements == null)
            {
                UnlockedAchievements = new List<string>();
            }

            if (!UnlockedAchievements.Contains(achievementId))
            {
                UnlockedAchievements.Add(achievementId);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Signed envelope wrapper providing SHA-256 integrity validation for profile.json (FR-40).
    /// </summary>
    [Serializable]
    public class ProfileEnvelope
    {
        public int Version = 1;
        public string Checksum = string.Empty;
        public string PayloadJson = string.Empty;

        public static ProfileEnvelope Create(ProfileData profile)
        {
            profile.LastSavedTimestamp = DateTime.UtcNow.ToString("o");
            string payload = JsonUtility.ToJson(profile, true);
            string checksum = ProfileChecksumUtility.ComputeChecksum(payload);

            return new ProfileEnvelope
            {
                Version = profile.SchemaVersion,
                Checksum = checksum,
                PayloadJson = payload
            };
        }

        public bool TryUnpack(out ProfileData profile, bool verifyChecksum = true)
        {
            profile = null;
            if (string.IsNullOrEmpty(PayloadJson))
            {
                return false;
            }

            if (verifyChecksum && !ProfileChecksumUtility.VerifyChecksum(PayloadJson, Checksum))
            {
                NullLog.Warn("Persistence", "Profile checksum validation failed! File may be tampered or corrupted.");
                return false;
            }

            try
            {
                profile = JsonUtility.FromJson<ProfileData>(PayloadJson);
                profile?.EnsureSubsectorRecords();
                return profile != null;
            }
            catch (Exception ex)
            {
                NullLog.Error("Persistence", $"Failed to deserialize profile payload: {ex.Message}");
                return false;
            }
        }
    }
}
