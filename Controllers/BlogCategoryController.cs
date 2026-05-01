using BackEnd.Data;
using BackEnd.Dtos.Blog;
using BackEnd.Helpers;
using BackEnd.Interfaces;
using BackEnd.Mappers;
using BackEnd.Modules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Controllers;

[Route("api/blog-categories")]
[ApiController]
public class BlogCategoryController : ControllerBase
{
    private readonly ApplicationDBContext _context;
    private readonly IBlogService _blogService;
    private readonly ILogger<BlogCategoryController> _logger;

    public BlogCategoryController(
        ApplicationDBContext context,
        IBlogService blogService,
        ILogger<BlogCategoryController> logger)
    {
        _context = context;
        _blogService = blogService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] string lang = "en")
    {
        var items = await _context.BlogCategories
            .OrderBy(c => c.Slug)
            .ToListAsync();
        _logger.LogInformation("GetAll blog categories: {Count}", items.Count);
        return Ok(new
        {
            success = true,
            data = items.Select(c => c.ToCategoryPublicDto(lang))
        });
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, [FromQuery] string lang = "en")
    {
        var category = await _context.BlogCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
            return NotFound(new { error = "Category not found" });
        return Ok(new { success = true, data = category.ToCategoryPublicDto(lang) });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] BlogCategoryCreateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Name is required" });

        var baseSlug = SlugHelper.Generate(
            string.IsNullOrWhiteSpace(dto.Slug) ? dto.Name : dto.Slug!,
            "category");
        var slug = SlugHelper.EnsureUnique(baseSlug, s => _context.BlogCategories.Any(c => c.Slug == s));

        var category = new BlogCategory
        {
            Slug = slug,
            Name = dto.Name.Trim(),
            OriginalLanguage = string.IsNullOrWhiteSpace(dto.OriginalLanguage) ? "en" : dto.OriginalLanguage!.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _blogService.PrepareCategoryTranslationsAsync(category);
        _context.BlogCategories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            new { success = true, data = category.ToCategoryAdminDto() });
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] BlogCategoryUpdateDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Name is required" });

        var category = await _context.BlogCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
            return NotFound(new { error = "Category not found" });

        var baseSlug = SlugHelper.Generate(
            string.IsNullOrWhiteSpace(dto.Slug) ? dto.Name : dto.Slug!,
            "category");
        var slug = SlugHelper.EnsureUnique(baseSlug, s => _context.BlogCategories.Any(c => c.Slug == s && c.Id != id));

        category.Slug = slug;
        category.Name = dto.Name.Trim();
        category.OriginalLanguage = string.IsNullOrWhiteSpace(dto.OriginalLanguage)
            ? category.OriginalLanguage
            : dto.OriginalLanguage.Trim();
        category.UpdatedAt = DateTime.UtcNow;

        await _blogService.PrepareCategoryTranslationsAsync(category);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = category.ToCategoryAdminDto() });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.BlogCategories.FindAsync(id);
        if (category == null)
            return NotFound(new { error = "Category not found" });

        _context.BlogCategories.Remove(category);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Category deleted" });
    }
}
