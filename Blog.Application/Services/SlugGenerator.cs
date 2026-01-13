using System.Globalization;
using System.Text;

namespace Blog.Application.Services;

public static class SlugGenerator
{
    public static string FromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var normalized = title.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(c);
        }

        var cleaned = builder.ToString().Normalize(NormalizationForm.FormC);
        var slug = new StringBuilder(cleaned.Length);

        foreach (var c in cleaned.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                slug.Append(c);
            }
            else if (char.IsWhiteSpace(c) || c == '-' || c == '_')
            {
                slug.Append('-');
            }
        }

        var result = slug.ToString().Trim('-');
        return string.Join('-', result.Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
