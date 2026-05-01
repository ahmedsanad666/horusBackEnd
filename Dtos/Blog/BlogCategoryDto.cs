namespace BackEnd.Dtos.Blog;

public class BlogCategoryCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? OriginalLanguage { get; set; }
}

public class BlogCategoryUpdateDto : BlogCategoryCreateDto
{
}

public class BlogCategoryPublicDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    /// <summary>Author input language for the category name (en or ar).</summary>
    public string OriginalLanguage { get; set; } = "en";
    public string Name { get; set; } = string.Empty;
    public string Lang { get; set; } = "en";
}

public class BlogCategoryAdminDto : BlogCategoryPublicDto
{
    /// <summary>Name as entered by the author (before translation).</summary>
    public string OriginalName { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsTranslated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
