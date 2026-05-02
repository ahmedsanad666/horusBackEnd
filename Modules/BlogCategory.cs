namespace BackEnd.Modules;

/// <summary>Blog category with EN/AR/TR display names (filled on write via DeepL).</summary>
public class BlogCategory
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    /// <summary>Original name as entered by the author.</summary>
    public string Name { get; set; } = string.Empty;
    public string OriginalLanguage { get; set; } = "en";
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public bool IsTranslated { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Blog> Blogs { get; set; } = new List<Blog>();
}
