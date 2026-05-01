namespace BackEnd.Interfaces;

public interface ITranslationService
{
    /// <summary>Translates text to DeepL target code (e.g. EN-US, AR). Empty input returns empty.</summary>
    Task<string> TranslateAsync(string text, string targetLanguageCode);

    bool IsConfigured { get; }
}
