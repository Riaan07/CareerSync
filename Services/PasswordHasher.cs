using System.Security.Cryptography;
using System.Text;

namespace CareerSync.Services
{

    /// <summary>
    /// Password hashing with PBKDF2-SHA256, a random 16-byte salt per password
    /// and 100,000 iterations. Stored format: "PBKDF2${iterations}${salt}${hash}".
    ///
    /// Backward compatible: accounts created before this change have an
    /// unsalted SHA-256 hex hash. Verify() still accepts those, and
    /// NeedsRehash() tells the login flow to upgrade them to PBKDF2 the next
    /// time the user signs in successfully — no one has to reset a password.
    /// </summary>
    public static class PasswordHasher
    {
        private const string Prefix = "PBKDF2$";
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Derive(password, salt, Iterations);
            return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string? storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;

            if (storedHash.StartsWith(Prefix, StringComparison.Ordinal))
            {
                var parts = storedHash.Substring(Prefix.Length).Split('$');
                if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations <= 0) return false;
                try
                {
                    var salt = Convert.FromBase64String(parts[1]);
                    var expected = Convert.FromBase64String(parts[2]);
                    return CryptographicOperations.FixedTimeEquals(Derive(password, salt, iterations), expected);
                }
                catch (FormatException)
                {
                    return false;
                }
            }

            // Legacy: unsalted SHA-256, upper-case hex (64 chars).
            if (storedHash.Length == 64)
            {
                var legacy = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(legacy),
                    Encoding.ASCII.GetBytes(storedHash.ToUpperInvariant()));
            }

            return false;
        }

        /// <summary>True for hashes in the old unsalted format.</summary>
        public static bool NeedsRehash(string? storedHash) =>
            storedHash is null || !storedHash.StartsWith(Prefix, StringComparison.Ordinal);

        private static byte[] Derive(string password, byte[] salt, int iterations) =>
            Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, KeySize);
    }
}

