using System;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Contract for persisting and loading profile.json locally (FR-40).
    /// </summary>
    public interface IProfileStorageService
    {
        string SaveFilePath { get; }
        string SaveDirectory { get; set; }
        bool Exists();
        bool Save(ProfileData profile);
        ProfileData Load();
        bool Delete();

        event Action<ProfileData> OnProfileSaved;
        event Action<ProfileData> OnProfileLoaded;
    }
}
