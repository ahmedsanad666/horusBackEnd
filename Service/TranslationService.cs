using BackEnd.Interfaces;
using DeepL;
using Microsoft.Extensions.Logging;

namespace BackEnd.Service;

public class TranslationService : ITranslationService
{
    private readonly Translator? _translator;
    private readonly ILogger<TranslationService> _logger;

    public bool IsConfigured => _translator != null;

    public TranslationService(IConfiguration configuration, ILogger<TranslationService> logger)
    {
        _logger = logger;
        var apiKey = configuration["DeepL:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("DeepL:ApiKey is not configured. Blog/category name translations will mirror the original language until a key is set.");
            return;
        }

        try
        {
            _translator = new Translator(apiKey.Trim());
            var keySuffix = apiKey.TrimEnd();
            if (keySuffix.EndsWith(":fx", StringComparison.OrdinalIgnoreCase))
                _logger.LogInformation("DeepL translator initialized (free tier key detected).");
            else
                _logger.LogInformation("DeepL translator initialized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create DeepL Translator instance.");
        }
    }

    public async Task<string> TranslateAsync(string text, string targetLanguageCode)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        if (_translator == null)
            return text;

        try
        {
            var result = await _translator.TranslateTextAsync(
                text,
                sourceLanguageCode: null,
                targetLanguageCode: targetLanguageCode);
            return result.Text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DeepL translation failed for target {Target}", targetLanguageCode);
            return text;
        }
    }
}
