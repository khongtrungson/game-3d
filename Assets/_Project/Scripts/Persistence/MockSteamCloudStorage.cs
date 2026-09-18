using System;
using System.Collections.Generic;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// In-memory implementation of ISteamCloudStorage for unit testing and offline simulation (FR-42).
    /// </summary>
    public class MockSteamCloudStorage : ISteamCloudStorage
    {
        private readonly Dictionary<string, (byte[] Data, DateTime Timestamp)> _cloudFiles = new Dictionary<string, (byte[], DateTime)>();
        private bool _isAvailable = true;

        public bool IsAvailable
        {
            get => _isAvailable;
            set => _isAvailable = value;
        }

        public int FileCount => _cloudFiles.Count;

        public bool FileWrite(string fileName, byte[] data)
        {
            if (!_isAvailable || string.IsNullOrEmpty(fileName) || data == null)
            {
                return false;
            }

            _cloudFiles[fileName] = (data, DateTime.UtcNow);
            return true;
        }

        public byte[] FileRead(string fileName)
        {
            if (!_isAvailable || string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            if (_cloudFiles.TryGetValue(fileName, out var entry))
            {
                return entry.Data;
            }

            return null;
        }

        public bool FileExists(string fileName)
        {
            if (!_isAvailable || string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            return _cloudFiles.ContainsKey(fileName);
        }

        public bool FileDelete(string fileName)
        {
            if (!_isAvailable || string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            return _cloudFiles.Remove(fileName);
        }

        public DateTime GetFileTimestamp(string fileName)
        {
            if (_cloudFiles.TryGetValue(fileName, out var entry))
            {
                return entry.Timestamp;
            }

            return DateTime.MinValue;
        }

        public int GetFileSize(string fileName)
        {
            if (_cloudFiles.TryGetValue(fileName, out var entry))
            {
                return entry.Data?.Length ?? 0;
            }

            return 0;
        }

        public void SetFileTimestamp(string fileName, DateTime timestamp)
        {
            if (_cloudFiles.TryGetValue(fileName, out var entry))
            {
                _cloudFiles[fileName] = (entry.Data, timestamp);
            }
        }

        public void Clear()
        {
            _cloudFiles.Clear();
        }
    }
}
