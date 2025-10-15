namespace TestSonioxLocal.Hubs;

public interface ICaptionClient
{
    // Old version - final only
    // Task ReceiveCaption(string caption);
    
    // New version - includes isFinal flag, speaker info, and isTranslation flag
    Task ReceiveCaption(string caption, bool isFinal, string? speaker, bool isTranslation);
    
    // Language settings update
    Task ReceiveLanguageUpdate(string sourceLanguage, string targetLanguage);
}
