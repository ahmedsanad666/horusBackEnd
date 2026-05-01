namespace BackEnd.Dtos.Blog;

public class BlogCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    /// <summary>Optional; generated from title when omitted.</summary>
    public string? Slug { get; set; }
    public string? ImageUrl { get; set; }
    /// <summary>Author language: en or ar.</summary>
    public string? OriginalLanguage { get; set; }
    public bool IsPublished { get; set; } = true;
    public List<int> CategoryIds { get; set; } = new();
}

public class BlogUpdateDto : BlogCreateDto
{
}

public class BlogPublicDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Lang { get; set; } = "en";
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BlogCategorySummaryDto> Categories { get; set; } = new();
}

public class BlogAdminDto : BlogPublicDto
{
    public string OriginalLanguage { get; set; } = "en";
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string ContentEn { get; set; } = string.Empty;
    public string ContentAr { get; set; } = string.Empty;
    public bool IsTranslated { get; set; }
}

public class BlogCategorySummaryDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class BlogImageUploadDto
{
    public IFormFile? ImageFile { get; set; }
}
