namespace BackEnd.Modules;

/// <summary>Blog post with original fields + EN/AR columns populated on create/update (DeepL).</summary>
public class Blog
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    /// <summary>Cover image URL (e.g. under wwwroot/images).</summary>
    public string? ImageUrl { get; set; }
    public string OriginalLanguage { get; set; } = "en";

    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string ContentEn { get; set; } = string.Empty;
    public string ContentAr { get; set; } = string.Empty;

    public bool IsTranslated { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<BlogCategory> Categories { get; set; } = new List<BlogCategory>();
}
