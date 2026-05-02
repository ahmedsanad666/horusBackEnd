using BackEnd.Interfaces;
using BackEnd.Modules;
using Microsoft.Extensions.Logging;

namespace BackEnd.Service;

public class PortfolioTranslationService : IPortfolioTranslationService
{
    private readonly ITranslationService _translation;
    private readonly ILogger<PortfolioTranslationService> _logger;

    public PortfolioTranslationService(
        ITranslationService translation,
        ILogger<PortfolioTranslationService> logger)
    {
        _translation = translation;
        _logger = logger;
    }

    public async Task PreparePortfolioTranslationsAsync(Portfolio portfolio)
    {
        var lang = NormalizeLanguage(portfolio.OriginalLanguage);
        portfolio.OriginalLanguage = lang;

        try
        {
            if (lang == "en")
            {
                portfolio.ArTitle = await _translation.TranslateAsync(portfolio.EnTitle, "AR");
                portfolio.TrTitle = await _translation.TranslateAsync(portfolio.EnTitle, "TR");
                portfolio.ArDescription = await _translation.TranslateAsync(
                    portfolio.EnDescription ?? string.Empty, "AR");
                portfolio.TrDescription = await _translation.TranslateAsync(
                    portfolio.EnDescription ?? string.Empty, "TR");
            }
            else if (lang == "ar")
            {
                portfolio.EnTitle = await _translation.TranslateAsync(portfolio.ArTitle, "EN-US");
                portfolio.TrTitle = await _translation.TranslateAsync(portfolio.ArTitle, "TR");
                portfolio.EnDescription = await _translation.TranslateAsync(
                    portfolio.ArDescription ?? string.Empty, "EN-US");
                portfolio.TrDescription = await _translation.TranslateAsync(
                    portfolio.ArDescription ?? string.Empty, "TR");
            }
            else
            {
                portfolio.EnTitle = await _translation.TranslateAsync(portfolio.TrTitle, "EN-US");
                portfolio.ArTitle = await _translation.TranslateAsync(portfolio.TrTitle, "AR");
                portfolio.EnDescription = await _translation.TranslateAsync(
                    portfolio.TrDescription ?? string.Empty, "EN-US");
                portfolio.ArDescription = await _translation.TranslateAsync(
                    portfolio.TrDescription ?? string.Empty, "AR");
            }

            portfolio.IsTranslated = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Portfolio translation pipeline failed.");
            portfolio.IsTranslated = false;
        }
    }

    private static string NormalizeLanguage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "en";
        var v = value.Trim().ToLowerInvariant();
        if (v.StartsWith("ar", StringComparison.Ordinal))
            return "ar";
        if (v.StartsWith("tr", StringComparison.Ordinal))
            return "tr";
        return "en";
    }
}
