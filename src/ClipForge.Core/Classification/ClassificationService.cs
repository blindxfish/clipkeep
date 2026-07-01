using System.Text.RegularExpressions;
using ClipForge.Core.Models;

namespace ClipForge.Core.Classification;

/// <summary>
/// Deterministic, rule-based classifier (no AI). Checks the most specific
/// single-token types first (url/email/phone/path/color), then code heuristics,
/// then long-text, falling back to plain text.
/// </summary>
public sealed partial class ClassificationService : IClassificationService
{
    private const int LongTextThreshold = 200;

    public ClipClassification Classify(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
            return new ClipClassification { Type = ClipType.Text };

        bool singleLine = !trimmed.Contains('\n');

        // Single-value types only make sense when the whole clip is that value.
        if (singleLine)
        {
            if (UrlRegex().IsMatch(trimmed))
            {
                var tags = new List<string>();
                var domain = ExtractDomain(trimmed);
                if (domain is not null) tags.Add(domain);
                return new ClipClassification { Type = ClipType.Url, Tags = tags };
            }

            if (EmailRegex().IsMatch(trimmed))
                return new ClipClassification { Type = ClipType.Email };

            if (ColorRegex().IsMatch(trimmed))
                return new ClipClassification { Type = ClipType.Color };

            if (FilePathRegex().IsMatch(trimmed))
                return new ClipClassification { Type = ClipType.FilePath };

            if (PhoneRegex().IsMatch(trimmed))
                return new ClipClassification { Type = ClipType.Phone };
        }

        var language = DetectCodeLanguage(trimmed);
        if (language is not null)
        {
            return new ClipClassification
            {
                Type = ClipType.Code,
                CodeLanguage = language,
                Tags = new[] { language }
            };
        }

        if (trimmed.Length > LongTextThreshold)
            return new ClipClassification { Type = ClipType.LongText };

        return new ClipClassification { Type = ClipType.Text };
    }

    private static string? ExtractDomain(string url)
    {
        var m = DomainRegex().Match(url);
        return m.Success ? m.Groups["host"].Value.ToLowerInvariant() : null;
    }

    /// <summary>
    /// Best-effort language detection using signature patterns. Returns null when
    /// nothing scores confidently enough to call it code.
    /// </summary>
    private static string? DetectCodeLanguage(string text)
    {
        // JSON: object/array that parses structurally enough to be obvious.
        if (JsonRegex().IsMatch(text)) return "json";
        // XML / HTML
        if (HtmlRegex().IsMatch(text)) return "html";
        if (XmlRegex().IsMatch(text)) return "xml";

        if (Regex.IsMatch(text, @"\busing\s+[A-Za-z_][\w.]*\s*;") ||
            Regex.IsMatch(text, @"\b(namespace|public|private|internal)\b.*\b(class|record|interface|struct)\b") ||
            text.Contains("Console.WriteLine"))
            return "csharp";

        if (Regex.IsMatch(text, @"\b(def|elif)\b") ||
            Regex.IsMatch(text, @"^\s*import\s+\w+", RegexOptions.Multiline) ||
            text.Contains("print("))
            return "python";

        if (Regex.IsMatch(text, @"\b(interface|type)\s+\w+\s*[={]") ||
            Regex.IsMatch(text, @":\s*(string|number|boolean)\b"))
            return "typescript";

        if (Regex.IsMatch(text, @"\b(const|let|var|function)\b") ||
            text.Contains("=>") || text.Contains("console.log"))
            return "javascript";

        if (Regex.IsMatch(text, @"\b(SELECT|INSERT|UPDATE|DELETE|CREATE\s+TABLE)\b.*\b(FROM|INTO|SET|VALUES|WHERE)\b",
                RegexOptions.IgnoreCase | RegexOptions.Singleline))
            return "sql";

        if (Regex.IsMatch(text, @"^\s*[$]?(Get|Set|New|Remove|Write|Import)-\w+", RegexOptions.Multiline) ||
            Regex.IsMatch(text, @"\$[A-Za-z_]\w*\s*="))
            return "powershell";

        if (Regex.IsMatch(text, @"^[\w-]+:\s*$", RegexOptions.Multiline) &&
            Regex.IsMatch(text, @"^\s+-?\s*\w+:", RegexOptions.Multiline))
            return "yaml";

        if (Regex.IsMatch(text, @"[.#]?[\w-]+\s*\{[^}]*:[^}]*;[^}]*\}", RegexOptions.Singleline))
            return "css";

        return null;
    }

    // --- URL: http(s):// or bare www. host ---
    [GeneratedRegex(@"^(https?://|www\.)[^\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"^(?:https?://)?(?:www\.)?(?<host>[a-z0-9.-]+\.[a-z]{2,})", RegexOptions.IgnoreCase)]
    private static partial Regex DomainRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    // Phone: optional +, digits/spaces/dashes/parens, at least 7 digits total.
    [GeneratedRegex(@"^\+?[\d\s().-]{7,}$")]
    private static partial Regex PhoneRegex();

    // Windows drive path (C:\...) or UNC (\\server\share...).
    [GeneratedRegex(@"^([a-zA-Z]:\\|\\\\)[^<>:""|?*\n]*$")]
    private static partial Regex FilePathRegex();

    [GeneratedRegex(@"^(#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})|rgba?\([^)]*\)|hsla?\([^)]*\))$")]
    private static partial Regex ColorRegex();

    [GeneratedRegex(@"^\s*[\{\[][\s\S]*[\}\]]\s*$")]
    private static partial Regex JsonRegex();

    [GeneratedRegex(@"<\s*(!doctype\s+html|html\b|div\b|span\b|body\b|p\b|a\b)", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlRegex();

    [GeneratedRegex(@"^\s*<\?xml|<[A-Za-z][\w:-]*(\s[^>]*)?>[\s\S]*<\/[A-Za-z]", RegexOptions.IgnoreCase)]
    private static partial Regex XmlRegex();
}
