using System;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Abstraction for Steam Cloud remote storage operations (FR-42).
    /// Decouples persistence systems from direct Steamworks SDK native dependencies,
    /// enabling mock execution, offline testing, and production cloud sync.
    /// </summary>
    public interface ISteamCloudStorage
    {
        bool IsAvailable { get; }
        bool FileWrite(string fileName, byte[] data);
        byte[] FileRead(string fileName);
        bool FileExists(string fileName);
        bool FileDelete(string fileName);
        DateTime GetFileTimestamp(string fileName);
        int GetFileSize(string fileName);
    }
}
