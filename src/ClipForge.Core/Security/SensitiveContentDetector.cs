using System.Text.RegularExpressions;

namespace ClipForge.Core.Security;

/// <summary>
/// Regex-based detection of secrets (credit cards, IBANs, JWTs, API/private keys).
/// Conservative by design: the default policy is to NOT persist sensitive content.
/// </summary>
public sealed partial class SensitiveContentDetector : ISensitiveContentDetector
{
    public bool IsSensitive(string text)
    {
        var t = text.Trim();
        if (t.Length == 0) return false;

        if (PrivateKeyRegex().IsMatch(t)) return true;
        if (JwtRegex().IsMatch(t)) return true;
        if (ApiKeyRegex().IsMatch(t)) return true;
        if (IbanRegex().IsMatch(t)) return true;
        if (LooksLikeCreditCard(t)) return true;

        return false;
    }

    private static bool LooksLikeCreditCard(string text)
    {
        foreach (Match m in CreditCardCandidateRegex().Matches(text))
        {
            var digits = m.Value.Where(char.IsDigit).ToArray();
            if (digits.Length is >= 13 and <= 19 && PassesLuhn(digits))
                return true;
        }
        return false;
    }

    private static bool PassesLuhn(IReadOnlyList<char> digits)
    {
        int sum = 0;
        bool dbl = false;
        for (int i = digits.Count - 1; i >= 0; i--)
        {
            int d = digits[i] - '0';
            if (dbl) { d *= 2; if (d > 9) d -= 9; }
            sum += d;
            dbl = !dbl;
        }
        return sum % 10 == 0;
    }

    [GeneratedRegex(@"-----BEGIN[ A-Z]*PRIVATE KEY-----")]
    private static partial Regex PrivateKeyRegex();

    // JWT: three base64url segments separated by dots.
    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b")]
    private static partial Regex JwtRegex();

    // Common API-key shapes: sk-..., ghp_..., AKIA..., or key/token=<long token>.
    [GeneratedRegex(@"(sk-[A-Za-z0-9]{20,}|gh[pousr]_[A-Za-z0-9]{20,}|AKIA[0-9A-Z]{16}|(api[_-]?key|secret|token)\s*[:=]\s*['""]?[A-Za-z0-9_\-]{20,})",
        RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex(@"\b[A-Z]{2}\d{2}[A-Z0-9]{11,30}\b")]
    private static partial Regex IbanRegex();

    // Runs of 13-19 digits possibly separated by spaces/dashes in groups.
    [GeneratedRegex(@"\b(?:\d[ -]?){13,19}\b")]
    private static partial Regex CreditCardCandidateRegex();
}
