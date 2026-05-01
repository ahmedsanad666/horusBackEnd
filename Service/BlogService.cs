using BackEnd.Interfaces;
using BackEnd.Modules;
using Microsoft.Extensions.Logging;

namespace BackEnd.Service;

public class BlogService : IBlogService
{
    private readonly ITranslationService _translation;
    private readonly ILogger<BlogService> _logger;

    public BlogService(ITranslationService translation, ILogger<BlogService> logger)
    {
        _translation = translation;
        _logger = logger;
    }

    public async Task PrepareBlogTranslationsAsync(Blog blog)
    {
        var lang = NormalizeLanguage(blog.OriginalLanguage);
        blog.OriginalLanguage = lang;

        try
        {
            if (lang == "en")
            {
                blog.TitleEn = blog.Title;
                blog.ContentEn = blog.Content;
                blog.DescriptionEn = blog.Description;
                blog.TitleAr = await _translation.TranslateAsync(blog.Title, "AR");
                blog.ContentAr = await _translation.TranslateAsync(blog.Content, "AR");
                blog.DescriptionAr = string.IsNullOrWhiteSpace(blog.Description)
                    ? null
                    : await _translation.TranslateAsync(blog.Description!, "AR");
            }
            else
            {
                blog.TitleAr = blog.Title;
                blog.ContentAr = blog.Content;
                blog.DescriptionAr = blog.Description;
                blog.TitleEn = await _translation.TranslateAsync(blog.Title, "EN-US");
                blog.ContentEn = await _translation.TranslateAsync(blog.Content, "EN-US");
                blog.DescriptionEn = string.IsNullOrWhiteSpace(blog.Description)
                    ? null
                    : await _translation.TranslateAsync(blog.Description!, "EN-US");
            }

            blog.IsTranslated = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blog translation pipeline failed; storing originals only where possible.");
            blog.IsTranslated = false;
        }
    }

    public async Task PrepareCategoryTranslationsAsync(BlogCategory category)
    {
        var lang = NormalizeLanguage(category.OriginalLanguage);
        category.OriginalLanguage = lang;

        try
        {
            if (lang == "en")
            {
                category.NameEn = category.Name;
                category.NameAr = await _translation.TranslateAsync(category.Name, "AR");
            }
            else
            {
                category.NameAr = category.Name;
                category.NameEn = await _translation.TranslateAsync(category.Name, "EN-US");
            }

            category.IsTranslated = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Category translation failed.");
            category.IsTranslated = false;
        }
    }

    private static string NormalizeLanguage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "en";
        var v = value.Trim().ToLowerInvariant();
        return v.StartsWith("ar", StringComparison.Ordinal) ? "ar" : "en";
    }
}
