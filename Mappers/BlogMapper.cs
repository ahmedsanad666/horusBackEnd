using BackEnd.Dtos.Blog;
using BackEnd.Modules;

namespace BackEnd.Mappers;

public static class BlogMapper
{
    public static BlogPublicDto ToPublicDto(this Blog blog, string lang, string? baseUrl)
    {
        var ar = lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var title = PickLocalized(ar, blog.TitleAr, blog.TitleEn);
        var desc = PickLocalizedNullable(ar, blog.DescriptionAr, blog.DescriptionEn);
        var content = PickLocalized(ar, blog.ContentAr, blog.ContentEn);
        var resolvedLang = ar ? "ar" : "en";

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
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt,
            Categories = blog.Categories?
                .OrderBy(c => c.Slug)
                .Select(c => c.ToSummaryDto(resolvedLang))
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
            DescriptionEn = blog.DescriptionEn,
            DescriptionAr = blog.DescriptionAr,
            ContentEn = blog.ContentEn,
            ContentAr = blog.ContentAr,
            IsTranslated = blog.IsTranslated
        };
    }

    public static BlogCategorySummaryDto ToSummaryDto(this BlogCategory category, string lang)
    {
        var ar = lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        return new BlogCategorySummaryDto
        {
            Id = category.Id,
            Slug = category.Slug,
            Name = PickLocalized(ar, category.NameAr, category.NameEn)
        };
    }

    public static BlogCategoryPublicDto ToCategoryPublicDto(this BlogCategory category, string lang)
    {
        var ar = lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        return new BlogCategoryPublicDto
        {
            Id = category.Id,
            Slug = category.Slug,
            OriginalLanguage = category.OriginalLanguage,
            Name = PickLocalized(ar, category.NameAr, category.NameEn),
            Lang = ar ? "ar" : "en"
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
            IsTranslated = category.IsTranslated,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    private static string PickLocalized(bool preferAr, string arValue, string enValue)
    {
        if (preferAr)
            return string.IsNullOrWhiteSpace(arValue) ? enValue : arValue;
        return string.IsNullOrWhiteSpace(enValue) ? arValue : enValue;
    }

    private static string? PickLocalizedNullable(bool preferAr, string? arValue, string? enValue)
    {
        var primary = preferAr ? arValue : enValue;
        var fallback = preferAr ? enValue : arValue;
        var chosen = string.IsNullOrWhiteSpace(primary) ? fallback : primary;
        return string.IsNullOrWhiteSpace(chosen) ? null : chosen;
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
