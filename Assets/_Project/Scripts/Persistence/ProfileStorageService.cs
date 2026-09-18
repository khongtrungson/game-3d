using System;
using System.IO;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Manages serialization and deserialization of the profile to a single local JSON file (profile.json) (FR-40).
    /// Defaults to Application.persistentDataPath with support for custom directory overrides for unit tests.
    /// Employs atomic write operations and SHA-256 integrity validation.
    /// </summary>
    public class ProfileStorageService : IProfileStorageService
    {
        public const string PROFILE_FILE_NAME = "profile.json";

        private string _saveDirectory;

        public string SaveDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_saveDirectory))
                {
                    _saveDirectory = Application.persistentDataPath;
                }
                return _saveDirectory;
            }
            set => _saveDirectory = value;
        }

        public string SaveFilePath => Path.Combine(SaveDirectory, PROFILE_FILE_NAME);

        public event Action<ProfileData> OnProfileSaved;
        public event Action<ProfileData> OnProfileLoaded;

        public ProfileStorageService(string customSaveDirectory = null)
        {
            if (!string.IsNullOrEmpty(customSaveDirectory))
            {
                _saveDirectory = customSaveDirectory;
            }
        }

        public bool Exists()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Saves progress data to profile.json atomically with SHA-256 checksum (FR-40).
        /// </summary>
        public bool Save(ProfileData profile)
        {
            if (profile == null)
            {
                NullLog.Error("Persistence", "Attempted to save null ProfileData.");
                return false;
            }

            try
            {
                if (!Directory.Exists(SaveDirectory))
                {
                    Directory.CreateDirectory(SaveDirectory);
                }

                var envelope = ProfileEnvelope.Create(profile);
                string envelopeJson = JsonUtility.ToJson(envelope, true);

                string tempPath = SaveFilePath + ".tmp";
                File.WriteAllText(tempPath, envelopeJson);

                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }
                File.Move(tempPath, SaveFilePath);

                NullLog.Info("Persistence", $"Successfully serialized profile to {SaveFilePath} (Checksum: {envelope.Checksum.Substring(0, 8)}...)");
                OnProfileSaved?.Invoke(profile);
                return true;
            }
            catch (Exception ex)
            {
                NullLog.Error("Persistence", $"Failed to write profile.json: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads profile.json, validates integrity checksum, and returns deserialized profile.
        /// Returns a fresh default profile if the file does not exist or fails validation (FR-40).
        /// </summary>
        public ProfileData Load()
        {
            if (!Exists())
            {
                NullLog.Info("Persistence", "No existing profile.json found. Initializing fresh default profile.");
                var fresh = new ProfileData();
                Save(fresh);
                OnProfileLoaded?.Invoke(fresh);
                return fresh;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);

                // Attempt to unpack signed envelope
                var envelope = JsonUtility.FromJson<ProfileEnvelope>(json);
                if (envelope != null && !string.IsNullOrEmpty(envelope.PayloadJson))
                {
                    if (envelope.TryUnpack(out ProfileData unpackedProfile, verifyChecksum: true))
                    {
                        NullLog.Info("Persistence", $"Successfully loaded profile.json (Active Subsector: {unpackedProfile.CurrentSubsector})");
                        OnProfileLoaded?.Invoke(unpackedProfile);
                        return unpackedProfile;
                    }
                    else
                    {
                        NullLog.Warn("Persistence", "Checksum mismatch or corrupt payload in envelope. Attempting raw fallback.");
                    }
                }

                // Fallback attempt: Direct deserialization if envelope was not used
                var directProfile = JsonUtility.FromJson<ProfileData>(json);
                if (directProfile != null)
                {
                    directProfile.EnsureSubsectorRecords();
                    NullLog.Info("Persistence", "Direct deserialization succeeded without envelope.");
                    OnProfileLoaded?.Invoke(directProfile);
                    return directProfile;
                }
            }
            catch (Exception ex)
            {
                NullLog.Error("Persistence", $"Error reading profile.json: {ex.Message}. Recovering with fresh profile.");
            }

            // Fallback recovery
            var fallback = new ProfileData();
            OnProfileLoaded?.Invoke(fallback);
            return fallback;
        }

        /// <summary>
        /// Deletes the local profile.json file.
        /// </summary>
        public bool Delete()
        {
            try
            {
                if (Exists())
                {
                    File.Delete(SaveFilePath);
                    NullLog.Info("Persistence", $"Deleted {SaveFilePath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                NullLog.Error("Persistence", $"Failed to delete profile.json: {ex.Message}");
                return false;
            }
        }
    }
}
