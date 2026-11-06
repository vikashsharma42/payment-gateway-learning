using System;
using System.Globalization;
using System.Security.Cryptography;

namespace DineBook.Application.Security
{
    /// <summary>
    /// PBKDF2-HMACSHA256 password hasher. Output format is
    /// "{iterations}.{base64(salt)}.{base64(hash)}" so the salt and iteration count travel with
    /// the hash itself - no separate columns needed.
    /// </summary>
    public sealed class PasswordHasher : IPasswordHasher
    {
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;
        private const int Iterations = 100_000;

        public string Hash(string plaintextPassword)
        {
            if (string.IsNullOrWhiteSpace(plaintextPassword))
                throw new ArgumentException("Password must not be empty.", nameof(plaintextPassword));

            var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            var hash = Rfc2898DeriveBytes.Pbkdf2(plaintextPassword, salt, Iterations, HashAlgorithmName.SHA256, HashSizeBytes);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool Verify(string plaintextPassword, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(plaintextPassword) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            var parts = hashedPassword.Split('.', 3);
            if (parts.Length != 3 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations))
            {
                return false;
            }

            byte[] salt;
            byte[] expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedHash = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(plaintextPassword, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
