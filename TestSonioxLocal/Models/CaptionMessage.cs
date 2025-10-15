namespace TestSonioxLocal.Models;

// Old version - final only
// public record CaptionMessage(string Text);

// New version - includes speaker, isFinal, and isTranslation flag
public record CaptionMessage(string Text, bool IsFinal, string? Speaker, bool IsTranslation);
