using BackEnd.Modules;

namespace BackEnd.Interfaces;

public interface IBlogService
{
    Task PrepareBlogTranslationsAsync(Blog blog);
    Task PrepareCategoryTranslationsAsync(BlogCategory category);
}
