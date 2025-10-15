using System.Text.Json.Serialization;

namespace TestSonioxLocal.Models;

public class Token
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("is_final")]
    public required bool IsFinal { get; set; }

    // Speaker identification (e.g., "S1", "S2", "S3")
    [JsonPropertyName("speaker")]
    public string? Speaker { get; set; }
    
    // Translation fields from Soniox API (for two-way translation)
    // translation_status: "none" (no translation), "original" (transcription to be translated), "translation" (translated text)
    [JsonPropertyName("translation_status")]
    public string? TranslationStatus { get; set; }
    
    // Language of this token (e.g., "sl" for Slovenian, "en" for English)
    [JsonPropertyName("language")]
    public string? Language { get; set; }
    
    // Source language (only present for translated tokens)
    [JsonPropertyName("source_language")]
    public string? SourceLanguage { get; set; }
}
