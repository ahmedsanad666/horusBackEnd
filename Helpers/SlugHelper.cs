using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BackEnd.Helpers;

public static class SlugHelper
{
    private static readonly Regex CollapseHyphens = new("-+", RegexOptions.Compiled);

    /// <summary>ASCII slug from title or name; falls back to <paramref name="fallbackPrefix"/> if empty.</summary>
    public static string Generate(string source, string fallbackPrefix = "item")
    {
        if (string.IsNullOrWhiteSpace(source))
            return fallbackPrefix;

        var normalized = source.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc == UnicodeCategory.NonSpacingMark)
                continue;
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
                sb.Append(c);
            else if (c is ' ' or '-' or '_')
                sb.Append('-');
        }

        var slug = CollapseHyphens.Replace(sb.ToString(), "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? fallbackPrefix : slug;
    }

    public static string EnsureUnique(string baseSlug, Func<string, bool> slugExists)
    {
        if (!slugExists(baseSlug))
            return baseSlug;

        var suffix = 2;
        string candidate;
        do
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        } while (slugExists(candidate));

        return candidate;
    }
}
