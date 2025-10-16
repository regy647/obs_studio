namespace TestSonioxLocal.Models;

public class UserSettings
{
    public string TextColor { get; set; } = "#ffffff";
    public int TextSize { get; set; } = 24;
    public float LineSpacing { get; set; } = 1.5f;
    public string ContainerColor { get; set; } = "rgba(0, 0, 0, 0.7)";
    public bool ShowTranscriptions { get; set; } = true;
    public bool ShowTranslations { get; set; } = true;
    public string SourceLanguage { get; set; } = "sl";
    public string TargetLanguage { get; set; } = "en";
    public bool TranscriptionToggle { get; set; } = true;
    public bool TranslationToggle { get; set; } = true;
}
