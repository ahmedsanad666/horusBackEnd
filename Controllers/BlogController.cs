using System.Security.Cryptography;
using System.Text;
using BackEnd.Data;
using BackEnd.Dtos.Blog;
using BackEnd.Helpers;
using BackEnd.Interfaces;
using BackEnd.Mappers;
using BackEnd.Modules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BackEnd.Controllers;

[Route("api/blogs")]
[ApiController]
public class BlogController : ControllerBase
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"];
    private static readonly TimeSpan ViewDedupWindow = TimeSpan.FromMinutes(30);

    private readonly ApplicationDBContext _context;
    private readonly IBlogService _blogService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BlogController> _logger;

    public BlogController(
        ApplicationDBContext context,
        IBlogService blogService,
        IMemoryCache cache,
        ILogger<BlogController> logger)
    {
        _context = context;
        _blogService = blogService;
        _cache = cache;
        _logger = logger;
    }

    /// <param name="lang">Locale for returned strings: en, ar, or tr (falls back to other stored translations).</param>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string lang = "en",
        [FromQuery] int? categoryId = null,
        [FromQuery] string? categorySlug = null,
        [FromQuery] bool includeDrafts = false)
    {
        var showDrafts = User?.Identity?.IsAuthenticated == true && includeDrafts;

        var query = _context.Blogs
            .Include(b => b.Categories)
            .AsQueryable();

        if (!showDrafts)
            query = query.Where(b => b.IsPublished);

        if (categoryId.HasValue)
            query = query.Where(b => b.Categories.Any(c => c.Id == categoryId.Value));

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            var slug = categorySlug.Trim().ToLowerInvariant();
            query = query.Where(b => b.Categories.Any(c => c.Slug == slug));
        }

        var list = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        _logger.LogInformation("GetAll blogs: {Count}", list.Count);

        return Ok(new
        {
            success = true,
            data = list.Select(b => b.ToPublicDto(lang, baseUrl))
        });
    }

    /// <param name="lang">Locale for returned strings: en, ar, or tr.</param>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, [FromQuery] string lang = "en")
    {
        var blog = await _context.Blogs
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (blog == null)
            return NotFound(new { error = "Blog not found" });

        if (!blog.IsPublished && User?.Identity?.IsAuthenticated != true)
            return NotFound(new { error = "Blog not found" });

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new { success = true, data = blog.ToPublicDto(lang, baseUrl) });
    }

    /// <param name="lang">Locale for returned strings: en, ar, or tr.</param>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug, [FromQuery] string lang = "en")
    {
        var key = slug.Trim().ToLowerInvariant();
        var blog = await _context.Blogs
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Slug == key);

        if (blog == null)
            return NotFound(new { error = "Blog not found" });

        if (!blog.IsPublished && User?.Identity?.IsAuthenticated != true)
            return NotFound(new { error = "Blog not found" });

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new { success = true, data = blog.ToPublicDto(lang, baseUrl) });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] BlogCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { error = "Title is required" });
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { error = "Content is required" });

        var baseSlug = SlugHelper.Generate(
            string.IsNullOrWhiteSpace(dto.Slug) ? dto.Title : dto.Slug!,
            "blog");
        var slug = SlugHelper.EnsureUnique(baseSlug, s => _context.Blogs.Any(b => b.Slug == s));

        var blog = new Blog
        {
            Slug = slug,
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Content = dto.Content.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
            OriginalLanguage = string.IsNullOrWhiteSpace(dto.OriginalLanguage) ? "en" : dto.OriginalLanguage!.Trim(),
            IsPublished = dto.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.CategoryIds is { Count: > 0 })
        {
            var categories = await _context.BlogCategories
                .Where(c => dto.CategoryIds.Contains(c.Id))
                .ToListAsync();
            foreach (var c in categories)
                blog.Categories.Add(c);
        }

        await _blogService.PrepareBlogTranslationsAsync(blog);
        _context.Blogs.Add(blog);
        await _context.SaveChangesAsync();

        await _context.Entry(blog).Collection(b => b.Categories).LoadAsync();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return CreatedAtAction(nameof(GetById), new { id = blog.Id },
            new { success = true, data = blog.ToAdminDto(baseUrl) });
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] BlogUpdateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { error = "Title is required" });
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { error = "Content is required" });

        var blog = await _context.Blogs
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (blog == null)
            return NotFound(new { error = "Blog not found" });

        var baseSlug = SlugHelper.Generate(
            string.IsNullOrWhiteSpace(dto.Slug) ? dto.Title : dto.Slug!,
            "blog");
        var slug = SlugHelper.EnsureUnique(baseSlug, s => _context.Blogs.Any(b => b.Slug == s && b.Id != id));

        blog.Slug = slug;
        blog.Title = dto.Title.Trim();
        blog.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        blog.Content = dto.Content.Trim();
        blog.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        blog.OriginalLanguage = string.IsNullOrWhiteSpace(dto.OriginalLanguage)
            ? blog.OriginalLanguage
            : dto.OriginalLanguage.Trim();
        blog.IsPublished = dto.IsPublished;
        blog.UpdatedAt = DateTime.UtcNow;

        blog.Categories.Clear();
        if (dto.CategoryIds is { Count: > 0 })
        {
            var categories = await _context.BlogCategories
                .Where(c => dto.CategoryIds.Contains(c.Id))
                .ToListAsync();
            foreach (var c in categories)
                blog.Categories.Add(c);
        }

        await _blogService.PrepareBlogTranslationsAsync(blog);
        await _context.SaveChangesAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new { success = true, data = blog.ToAdminDto(baseUrl) });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var blog = await _context.Blogs
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (blog == null)
            return NotFound(new { error = "Blog not found" });

        TryDeleteBlogImageFileFromDisk(blog.ImageUrl);
        blog.Categories.Clear();
        _context.Blogs.Remove(blog);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Blog deleted" });
    }

    /// <summary>Upload cover image; replaces ImageUrl with an absolute URL.</summary>
    [HttpPut("{id:int}/cover")]
    [Authorize]
    public async Task<IActionResult> SetCover(int id, [FromForm] BlogImageUploadDto dto)
    {
        var blog = await _context.Blogs.FindAsync(id);
        if (blog == null)
            return NotFound(new { error = "Blog not found" });

        if (dto?.ImageFile == null || dto.ImageFile.Length == 0)
            return BadRequest(new { error = "No file was uploaded." });

        var ext = Path.GetExtension(dto.ImageFile.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
            return BadRequest(new { error = "Invalid file type. Only image files are allowed." });

        TryDeleteBlogImageFileFromDisk(blog.ImageUrl);

        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        Directory.CreateDirectory(uploadPath);
        var fileName = Guid.NewGuid() + ext;
        var filePath = Path.Combine(uploadPath, fileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
            await dto.ImageFile.CopyToAsync(stream);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        blog.ImageUrl = $"{baseUrl}/images/{fileName}";
        blog.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = blog.ToAdminDto(baseUrl) });
    }

    /// <summary>Register a public view for a published blog. Dedup'd per (blog + IP + UA) for 30 minutes.</summary>
    [HttpPost("{id:int}/view")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterView(int id)
    {
        var blog = await _context.Blogs
            .Where(b => b.Id == id && b.IsPublished)
            .Select(b => new { b.Id, b.ViewsCount })
            .FirstOrDefaultAsync();

        if (blog == null)
            return NotFound(new { error = "Blog not found" });

        var ip = GetClientIp();
        var ua = Request.Headers.UserAgent.ToString();
        var fingerprint = $"{ip}|{ua}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
        var cacheKey = $"blog:view:{id}:{hash}";

        if (_cache.TryGetValue(cacheKey, out _))
            return Ok(new { success = true, data = new { id, viewsCount = blog.ViewsCount } });

        await _context.Blogs
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.ViewsCount, b => b.ViewsCount + 1));

        _cache.Set(cacheKey, true, ViewDedupWindow);

        return Ok(new { success = true, data = new { id, viewsCount = blog.ViewsCount + 1 } });
    }

    private string GetClientIp()
    {
        var fwd = Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(fwd))
            return fwd.Split(',')[0].Trim();
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static void TryDeleteBlogImageFileFromDisk(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        string fileName;
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
            fileName = Path.GetFileName(absoluteUri.LocalPath);
        else
            fileName = Path.GetFileName(imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (string.IsNullOrEmpty(fileName))
            return;

        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", fileName);
        try
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
        catch
        {
            /* ignore disk cleanup failures */
        }
    }
}
