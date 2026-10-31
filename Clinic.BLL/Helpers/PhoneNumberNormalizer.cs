using System.Text.RegularExpressions;

namespace Clinic.BLL.Helpers;

public static class PhoneNumberNormalizer
{
    private static readonly Regex EgyptianMobilePattern =
        new(@"^01[0125][0-9]{8}$", RegexOptions.Compiled);

    /// <summary>
    /// Normalizes Egyptian mobile phone numbers to the canonical 11-digit local format: 01XXXXXXXXX.
    /// Handles prefixes such as +20, 0020, and 20, as well as spaces, dashes, dots, and parentheses.
    /// Returns true if the phone number is valid and normalized; otherwise false.
    /// </summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Strip whitespace, dashes, dots, parentheses, and slashes
        var cleaned = Regex.Replace(input.Trim(), @"[\s\-\(\)\.\/\\]", "");

        // Convert international Egyptian country code to leading 0
        if (cleaned.StartsWith("+20"))
        {
            var remainder = cleaned[3..];
            cleaned = "0" + (remainder.StartsWith('0') ? remainder[1..] : remainder);
        }
        else if (cleaned.StartsWith("0020"))
        {
            var remainder = cleaned[4..];
            cleaned = "0" + (remainder.StartsWith('0') ? remainder[1..] : remainder);
        }
        else if (cleaned.Length == 12 && cleaned.StartsWith("20"))
        {
            var remainder = cleaned[2..];
            cleaned = "0" + (remainder.StartsWith('0') ? remainder[1..] : remainder);
        }

        if (EgyptianMobilePattern.IsMatch(cleaned))
        {
            normalized = cleaned;
            return true;
        }

        return false;
    }

    public static bool IsValid(string? input) => TryNormalize(input, out _);

    public static string Normalize(string input)
    {
        if (TryNormalize(input, out var normalized))
        {
            return normalized;
        }

        throw new ArgumentException($"'{input}' is not a valid Egyptian mobile number.", nameof(input));
    }
}

