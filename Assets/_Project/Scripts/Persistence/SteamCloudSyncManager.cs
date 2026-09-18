using System;
using System.IO;
using System.Text;
using UnityEngine;
using NullProtocol.Core;

namespace NullProtocol.Persistence
{
    public enum CloudSyncResult
    {
        CloudUnavailable,
        UploadedToCloud,
        DownloadedFromCloud,
        AlreadyInSync,
        Error
    }

    /// <summary>
    /// Coordinates Steam Cloud synchronization for profile.json with conflict resolution (FR-42).
    /// Resolves discrepancies between local disk persistence and remote Steam Cloud storage.
    /// </summary>
    public class SteamCloudSyncManager
    {
        private readonly IProfileStorageService _localService;
        private readonly ISteamCloudStorage _cloudStorage;

        public event Action<CloudSyncResult> OnSyncCompleted;

        public SteamCloudSyncManager(IProfileStorageService localService, ISteamCloudStorage cloudStorage)
        {
            _localService = localService ?? throw new ArgumentNullException(nameof(localService));
            _cloudStorage = cloudStorage ?? throw new ArgumentNullException(nameof(cloudStorage));
        }

        /// <summary>
        /// Executes two-way synchronization between local profile.json and Steam Cloud (FR-42).
        /// </summary>
        public CloudSyncResult Synchronize()
        {
            if (_cloudStorage == null || !_cloudStorage.IsAvailable)
            {
                NullLog.Info("SteamCloud", "Steam Cloud synchronization skipped: cloud storage unavailable.");
                OnSyncCompleted?.Invoke(CloudSyncResult.CloudUnavailable);
                return CloudSyncResult.CloudUnavailable;
            }

            try
            {
                bool localExists = _localService.Exists();
                bool cloudExists = _cloudStorage.FileExists(ProfileStorageService.PROFILE_FILE_NAME);

                // Case 1: Both missing
                if (!localExists && !cloudExists)
                {
                    OnSyncCompleted?.Invoke(CloudSyncResult.AlreadyInSync);
                    return CloudSyncResult.AlreadyInSync;
                }

                // Case 2: Only local exists -> Push to Cloud
                if (localExists && !cloudExists)
                {
                    return PushToCloud();
                }

                // Case 3: Only cloud exists -> Pull from Cloud
                if (!localExists && cloudExists)
                {
                    return PullFromCloud();
                }

                // Case 4: Both exist -> Conflict resolution based on UTC timestamp
                DateTime localTime = File.GetLastWriteTimeUtc(_localService.SaveFilePath);
                DateTime cloudTime = _cloudStorage.GetFileTimestamp(ProfileStorageService.PROFILE_FILE_NAME);

                TimeSpan diff = cloudTime - localTime;
                if (diff.TotalSeconds > 1.5)
                {
                    // Cloud version is strictly newer
                    NullLog.Info("SteamCloud", $"Cloud profile is newer ({cloudTime:O} > {localTime:O}). Pulling from cloud.");
                    return PullFromCloud();
                }
                else if (diff.TotalSeconds < -1.5)
                {
                    // Local version is strictly newer
                    NullLog.Info("SteamCloud", $"Local profile is newer ({localTime:O} > {cloudTime:O}). Pushing to cloud.");
                    return PushToCloud();
                }
                else
                {
                    // Timestamps are virtually identical, compare payloads
                    byte[] localBytes = File.ReadAllBytes(_localService.SaveFilePath);
                    byte[] cloudBytes = _cloudStorage.FileRead(ProfileStorageService.PROFILE_FILE_NAME);

                    if (ByteArraysEqual(localBytes, cloudBytes))
                    {
                        NullLog.Info("SteamCloud", "Local and Cloud profile files are identical.");
                        OnSyncCompleted?.Invoke(CloudSyncResult.AlreadyInSync);
                        return CloudSyncResult.AlreadyInSync;
                    }

                    // Default to local if payloads differ slightly at same second
                    return PushToCloud();
                }
            }
            catch (Exception ex)
            {
                NullLog.Error("SteamCloud", $"Synchronization exception: {ex.Message}");
                OnSyncCompleted?.Invoke(CloudSyncResult.Error);
                return CloudSyncResult.Error;
            }
        }

        /// <summary>
        /// Pushes the local profile.json to Steam Cloud storage (FR-42).
        /// </summary>
        public CloudSyncResult PushToCloud()
        {
            if (_cloudStorage == null || !_cloudStorage.IsAvailable)
            {
                return CloudSyncResult.CloudUnavailable;
            }

            if (!_localService.Exists())
            {
                return CloudSyncResult.Error;
            }

            try
            {
                byte[] localBytes = File.ReadAllBytes(_localService.SaveFilePath);
                bool success = _cloudStorage.FileWrite(ProfileStorageService.PROFILE_FILE_NAME, localBytes);

                if (success)
                {
                    NullLog.Info("SteamCloud", $"Successfully uploaded {localBytes.Length} bytes to Steam Cloud.");
                    OnSyncCompleted?.Invoke(CloudSyncResult.UploadedToCloud);
                    return CloudSyncResult.UploadedToCloud;
                }

                NullLog.Warn("SteamCloud", "Failed to upload profile to Steam Cloud.");
                OnSyncCompleted?.Invoke(CloudSyncResult.Error);
                return CloudSyncResult.Error;
            }
            catch (Exception ex)
            {
                NullLog.Error("SteamCloud", $"PushToCloud exception: {ex.Message}");
                OnSyncCompleted?.Invoke(CloudSyncResult.Error);
                return CloudSyncResult.Error;
            }
        }

        /// <summary>
        /// Pulls profile.json from Steam Cloud and overwrites local disk file (FR-42).
        /// </summary>
        public CloudSyncResult PullFromCloud()
        {
            if (_cloudStorage == null || !_cloudStorage.IsAvailable)
            {
                return CloudSyncResult.CloudUnavailable;
            }

            if (!_cloudStorage.FileExists(ProfileStorageService.PROFILE_FILE_NAME))
            {
                return CloudSyncResult.Error;
            }

            try
            {
                byte[] cloudBytes = _cloudStorage.FileRead(ProfileStorageService.PROFILE_FILE_NAME);
                if (cloudBytes == null || cloudBytes.Length == 0)
                {
                    return CloudSyncResult.Error;
                }

                string dir = _localService.SaveDirectory;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllBytes(_localService.SaveFilePath, cloudBytes);
                DateTime cloudTime = _cloudStorage.GetFileTimestamp(ProfileStorageService.PROFILE_FILE_NAME);
                if (cloudTime > DateTime.MinValue)
                {
                    File.SetLastWriteTimeUtc(_localService.SaveFilePath, cloudTime);
                }

                NullLog.Info("SteamCloud", $"Successfully pulled {cloudBytes.Length} bytes from Steam Cloud to local storage.");
                OnSyncCompleted?.Invoke(CloudSyncResult.DownloadedFromCloud);
                return CloudSyncResult.DownloadedFromCloud;
            }
            catch (Exception ex)
            {
                NullLog.Error("SteamCloud", $"PullFromCloud exception: {ex.Message}");
                OnSyncCompleted?.Invoke(CloudSyncResult.Error);
                return CloudSyncResult.Error;
            }
        }

        private static bool ByteArraysEqual(byte[] b1, byte[] b2)
        {
            if (ReferenceEquals(b1, b2)) return true;
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }
    }
}
