using Microsoft.AspNetCore.SignalR;
using TestSonioxLocal.Services;

namespace TestSonioxLocal.Hubs;

public class CaptionHub : Hub<ICaptionClient>
{
    private readonly InitService _initService;
    private readonly ILogger<CaptionHub> _logger;
    
    public CaptionHub(
        InitService initService,
        ILogger<CaptionHub> logger)
    {
        _initService = initService;
        _logger = logger;
    }
    
    public async Task SendCaption(string caption, bool isFinal, string? speaker, bool isTranslation)
    {
        await Clients.All.ReceiveCaption(caption, isFinal, speaker, isTranslation);
    }
    
    public async Task UpdateLanguageSettings(string sourceLanguage, string targetLanguage)
    {
        _logger.LogInformation($"[CAPTION HUB] Received UpdateLanguageSettings: {sourceLanguage} → {targetLanguage}");
        
        try
        {
            // RESTART ALL SERVICES with new languages 
            _logger.LogInformation("[CAPTION HUB] Restarting all services with new languages...");
            
            await _initService.RestartAllServices(sourceLanguage, targetLanguage);
            
            _logger.LogInformation("[CAPTION HUB] ✅ All services restarted successfully!");
            
            // Notify all clients about the language change
            _logger.LogInformation("[CAPTION HUB] Notifying all clients about language change...");
            await Clients.All.ReceiveLanguageUpdate(sourceLanguage, targetLanguage);
            _logger.LogInformation("[CAPTION HUB] All clients notified");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CAPTION HUB] Error updating language settings: {ex.Message}");
            throw;
        }
    }
}
