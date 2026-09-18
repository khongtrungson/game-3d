using System;
using UnityEngine;
using NullProtocol.World;

namespace NullProtocol.UI
{
    public enum TacticalGrade
    {
        S,
        A,
        B,
        C
    }

    /// <summary>
    /// Encapsulates level performance metrics presented on the Tactical Debrief screen (FR-33).
    /// </summary>
    [Serializable]
    public struct TacticalDebriefData
    {
        public SubsectorId Subsector;
        public string SubsectorTitle;
        public float ElapsedTimeSeconds;
        public int ShotsFired;
        public int ShotsHit;
        public float AccuracyPercentage;
        public int DamageTaken;
        public TacticalGrade Grade;
        public int EnemiesNeutralized;
        public int HeadshotsCount;

        public string FormattedTime
        {
            get
            {
                int totalCentis = Mathf.RoundToInt(ElapsedTimeSeconds * 100f);
                int centis = totalCentis % 100;
                int totalSeconds = totalCentis / 100;
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                return $"{minutes:00}:{seconds:00}.{centis:00}";
            }
        }

        public string FormattedAccuracy => $"{AccuracyPercentage:F1}%";
        public string FormattedDamage => $"{DamageTaken} HP";
        public float HeadshotPercentage => (ShotsHit > 0) ? ((float)HeadshotsCount / ShotsHit * 100f) : 0f;
        public string FormattedHeadshots => $"{HeadshotPercentage:F1}%";
        public string GradeString => Grade.ToString();

        public TacticalDebriefData(
            SubsectorId subsector,
            string title,
            float elapsedTime,
            int shotsFired,
            int shotsHit,
            int damageTaken,
            TacticalGrade grade,
            int enemiesNeutralized = 0,
            int headshots = 0)
        {
            Subsector = subsector;
            SubsectorTitle = title;
            ElapsedTimeSeconds = elapsedTime;
            ShotsFired = shotsFired;
            ShotsHit = shotsHit;
            AccuracyPercentage = (shotsFired > 0) ? ((float)shotsHit / shotsFired * 100f) : 100f;
            DamageTaken = damageTaken;
            Grade = grade;
            EnemiesNeutralized = enemiesNeutralized;
            HeadshotsCount = headshots;
        }
    }

    /// <summary>
    /// Computes tactical grades (S, A, B, C) based on mission performance metrics (FR-33).
    /// </summary>
    public static class TacticalGradeCalculator
    {
        /// <summary>
        /// Calculates the tactical grade:
        /// - Grade S: Flawless/Surgical (Damage <= 25, Accuracy >= 70%, Time <= ParTime * 1.3, or Zero Damage with >= 60% accuracy)
        /// - Grade A: Superior Tactical (Damage <= 60, Accuracy >= 50%)
        /// - Grade B: Standard Operative (Damage <= 100, Accuracy >= 30%)
        /// - Grade C: Compromised / Heavy Casualties
        /// </summary>
        public static TacticalGrade CalculateGrade(
            float elapsedTime,
            float accuracyPercentage,
            int damageTaken,
            float parTimeSeconds = 120.0f)
        {
            // Zero damage run with competent accuracy awards automatic S
            if (damageTaken == 0 && accuracyPercentage >= 60.0f)
            {
                return TacticalGrade.S;
            }

            // S-Grade: Low damage, high accuracy, within reasonable par time
            bool isFastEnoughForS = parTimeSeconds <= 0f || elapsedTime <= parTimeSeconds * 1.35f;
            if (damageTaken <= 25 && accuracyPercentage >= 70.0f && isFastEnoughForS)
            {
                return TacticalGrade.S;
            }

            // A-Grade: Moderate damage, solid accuracy
            if (damageTaken <= 60 && accuracyPercentage >= 50.0f)
            {
                return TacticalGrade.A;
            }

            // B-Grade: Survivable damage, fair accuracy
            if (damageTaken <= 100 && accuracyPercentage >= 30.0f)
            {
                return TacticalGrade.B;
            }

            return TacticalGrade.C;
        }
    }
}
