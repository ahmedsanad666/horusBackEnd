using BackEnd.Modules;

namespace BackEnd.Interfaces;

public interface IPortfolioTranslationService
{
    /// <summary>Fills En/Ar/Tr title and description from <see cref="Portfolio.OriginalLanguage"/> using DeepL.</summary>
    Task PreparePortfolioTranslationsAsync(Portfolio portfolio);
}
