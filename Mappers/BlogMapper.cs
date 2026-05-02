using BackEnd.Dtos.Blog;
using BackEnd.Modules;

namespace BackEnd.Mappers;

public static class BlogMapper
{
    public static BlogPublicDto ToPublicDto(this Blog blog, string lang, string? baseUrl)
    {
        var resolvedLang = ResolvePublicLangCode(lang);
        var title = PickForLang(lang, blog.TitleEn, blog.TitleAr, blog.TitleTr);
        var desc = PickForLangNullable(lang, blog.DescriptionEn, blog.DescriptionAr, blog.DescriptionTr);
        var content = PickForLang(lang, blog.ContentEn, blog.ContentAr, blog.ContentTr);

        return new BlogPublicDto
        {
            Id = blog.Id,
            Slug = blog.Slug,
            OriginalLanguage = blog.OriginalLanguage,
            Title = title,
            Description = desc,
            Content = content,
            ImageUrl = ResolveUrl(blog.ImageUrl, baseUrl),
            Lang = resolvedLang,
            IsPublished = blog.IsPublished,
            ViewsCount = blog.ViewsCount,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt,
            Categories = blog.Categories?
                .OrderBy(c => c.Slug)
                .Select(c => c.ToSummaryDto(lang))
                .ToList() ?? new List<BlogCategorySummaryDto>()
        };
    }

    public static BlogAdminDto ToAdminDto(this Blog blog, string? baseUrl)
    {
        return new BlogAdminDto
        {
            Id = blog.Id,
            Slug = blog.Slug,
            Title = blog.Title,
            Description = blog.Description,
            Content = blog.Content,
            ImageUrl = ResolveUrl(blog.ImageUrl, baseUrl),
            Lang = blog.OriginalLanguage,
            IsPublished = blog.IsPublished,
            ViewsCount = blog.ViewsCount,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt,
            Categories = blog.Categories?
                .OrderBy(c => c.Slug)
                .Select(c => new BlogCategorySummaryDto
                {
                    Id = c.Id,
                    Slug = c.Slug,
                    Name = c.Name
                })
                .ToList() ?? new List<BlogCategorySummaryDto>(),
            OriginalLanguage = blog.OriginalLanguage,
            TitleEn = blog.TitleEn,
            TitleAr = blog.TitleAr,
            TitleTr = blog.TitleTr,
            DescriptionEn = blog.DescriptionEn,
            DescriptionAr = blog.DescriptionAr,
            DescriptionTr = blog.DescriptionTr,
            ContentEn = blog.ContentEn,
            ContentAr = blog.ContentAr,
            ContentTr = blog.ContentTr,
            IsTranslated = blog.IsTranslated
        };
    }

    public static BlogCategorySummaryDto ToSummaryDto(this BlogCategory category, string lang)
    {
        return new BlogCategorySummaryDto
        {
            Id = category.Id,
            Slug = category.Slug,
            Name = PickForLang(lang, category.NameEn, category.NameAr, category.NameTr)
        };
    }

    public static BlogCategoryPublicDto ToCategoryPublicDto(this BlogCategory category, string lang)
    {
        return new BlogCategoryPublicDto
        {
            Id = category.Id,
            Slug = category.Slug,
            OriginalLanguage = category.OriginalLanguage,
            Name = PickForLang(lang, category.NameEn, category.NameAr, category.NameTr),
            Lang = ResolvePublicLangCode(lang)
        };
    }

    public static BlogCategoryAdminDto ToCategoryAdminDto(this BlogCategory category)
    {
        var pub = category.ToCategoryPublicDto("en");
        return new BlogCategoryAdminDto
        {
            Id = pub.Id,
            Slug = pub.Slug,
            Name = pub.Name,
            Lang = pub.Lang,
            OriginalName = category.Name,
            OriginalLanguage = category.OriginalLanguage,
            NameEn = category.NameEn,
            NameAr = category.NameAr,
            NameTr = category.NameTr,
            IsTranslated = category.IsTranslated,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    /// <summary>Normalized lang tag for API consumers (en, ar, or tr).</summary>
    private static string ResolvePublicLangCode(string lang)
    {
        var l = (lang ?? "en").Trim().ToLowerInvariant();
        if (l.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
            return "ar";
        if (l.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
            return "tr";
        return "en";
    }

    private static string PickForLang(string lang, string enValue, string arValue, string trValue)
    {
        var l = (lang ?? "en").Trim().ToLowerInvariant();
        if (l.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(arValue)) return arValue;
            if (!string.IsNullOrWhiteSpace(enValue)) return enValue;
            return trValue;
        }

        if (l.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(trValue)) return trValue;
            if (!string.IsNullOrWhiteSpace(enValue)) return enValue;
            return arValue;
        }

        if (!string.IsNullOrWhiteSpace(enValue)) return enValue;
        if (!string.IsNullOrWhiteSpace(arValue)) return arValue;
        return trValue;
    }

    private static string? PickForLangNullable(string lang, string? enValue, string? arValue, string? trValue)
    {
        var s = PickForLang(lang, enValue ?? "", arValue ?? "", trValue ?? "");
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static string? ResolveUrl(string? url, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;
        if (!string.IsNullOrEmpty(baseUrl))
            return baseUrl.TrimEnd('/') + (url.StartsWith('/') ? url : "/" + url);
        return url;
    }
}
