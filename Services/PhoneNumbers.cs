using System.Text.RegularExpressions;

namespace CareerSync.Services;

public static class PhoneNumbers
{
    public const string DefaultCountryCode = "27";   // South Africa

    private static readonly Regex E164 = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

    /// <summary>Returns the number in E.164 format, or null if it can't be converted.</summary>
    public static string? ToE164(string? raw, string defaultCountryCode = DefaultCountryCode)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var trimmed = raw.Trim();
        var hasPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        string candidate;
        if (hasPlus)
            candidate = "+" + digits;                                       // +27 82 345 6789
        else if (digits.StartsWith("00"))
            candidate = "+" + digits[2..];                                  // 0027 82 345 6789
        else if (digits.StartsWith('0'))
            candidate = "+" + defaultCountryCode + digits[1..];             // 082 345 6789 (local)
        else if (digits.StartsWith(defaultCountryCode) && digits.Length == defaultCountryCode.Length + 9)
            candidate = "+" + digits;                                       // 27823456789
        else
            return null;                                                    // ambiguous

        // South African numbers are always +27 followed by exactly 9 digits.
        if (candidate.StartsWith("+27") && candidate.Length != 12) return null;

        return E164.IsMatch(candidate) ? candidate : null;
    }

    public static string Mask(string e164) =>
        e164.Length < 8 ? "••••" : $"{e164[..3]} •• ••• {e164[^4..]}";
}
