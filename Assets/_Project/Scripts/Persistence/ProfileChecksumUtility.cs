using System;
using System.Security.Cryptography;
using System.Text;

namespace NullProtocol.Persistence
{
    /// <summary>
    /// Utility for calculating and validating SHA-256 integrity checksums for profile persistence (FR-40).
    /// Detects save data tampering and file corruption.
    /// </summary>
    public static class ProfileChecksumUtility
    {
        private const string SALT = "NullProtocol_Core_Simulation_Salt_v1";

        /// <summary>
        /// Computes SHA256 checksum string for the provided raw JSON data.
        /// </summary>
        public static string ComputeChecksum(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return string.Empty;
            }

            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(payload + SALT);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                var sb = new StringBuilder(hashBytes.Length * 2);
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Validates that the payload matches the expected checksum.
        /// </summary>
        public static bool VerifyChecksum(string payload, string expectedChecksum)
        {
            if (string.IsNullOrEmpty(expectedChecksum))
            {
                return false;
            }

            string computed = ComputeChecksum(payload);
            return string.Equals(computed, expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }
    }
}
