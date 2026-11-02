using System.Security.Cryptography;
using System.Text;

namespace CareerSync.Services
{
    /// <summary>
    /// Password-reset tokens. Once the user has proved who they are (SMS code or
    /// security questions) the raw token goes to them in the redirect to the
    /// "Set a new password" page; only its
    /// SHA-256 hash is stored in AppUser.ResetToken. Someone who reads the
    /// database therefore can't use a stored value to reset a password.
    /// A token is 32 random bytes, so a fast hash is fine here (unlike passwords).
    /// </summary>
    public static class PasswordResetTokens
    {
        /// <summary>How long the user has to choose a new password after proving
        /// who they are (correct SMS code or correct security answers).</summary>
        public static readonly TimeSpan VerifiedResetLifetime = TimeSpan.FromMinutes(15);

        /// <summary>Creates a new token. Send <c>Raw</c> to the user, store <c>Hash</c>.</summary>
        public static (string Raw, string Hash) Create()
        {
            // URL-safe base64 so it survives being put in a link unchanged.
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return (raw, Hash(raw));
        }

        public static string Hash(string rawToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        /// <summary>Constant-time check of a token from a link/form against the stored hash.</summary>
        public static bool Matches(string? rawToken, string? storedHash)
        {
            if (string.IsNullOrEmpty(rawToken) || string.IsNullOrEmpty(storedHash)) return false;
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(Hash(rawToken)),
                Encoding.ASCII.GetBytes(storedHash));
        }
    }
}
