using TestSonioxLocal.Models.Enums;
using System.Threading.Channels;
using TestSonioxLocal.Models;
using TestSonioxLocal.Services.HttpClients;
using TestSonioxLocal.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace TestSonioxLocal.Services;

public class InitService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISonioxHttpClient _sonioxHttpClient;
    private IServiceScope? _scope;
    private ISonioxWsService? _sonioxWsService;
    private ICaptureAudioService? _captureAudioService;
    private ICaptionService? _captionService;
    private readonly ILogger<InitService> _logger;

    private CancellationTokenSource? _currentCts;
    private List<Task> _runningTasks = new();
    
    // FRESH channels for each restart - this is the key fix!
    private Channel<WsMessage>? _loopbackSendChannel;
    private Channel<WsMessage>? _micSendChannel;
    private Channel<CaptionMessage>? _captionChannel;

    public InitService(
        IServiceScopeFactory scopeFactory, 
        ISonioxHttpClient sonioxHttpClient,
        ILogger<InitService> logger)
    {
        _scopeFactory = scopeFactory;
        _sonioxHttpClient = sonioxHttpClient;
        _logger = logger;
    }

    public async Task RestartAllServices(string sourceLanguage, string targetLanguage)
    {
        _logger.LogInformation($"🔄 RESTARTING ALL SERVICES with new languages: {sourceLanguage} → {targetLanguage}");
        
        // Stop all current services
        if (_currentCts != null)
        {
            _logger.LogInformation("Stopping current services...");
            _currentCts.Cancel();
            
            // Wait for tasks to complete (with timeout)
            try
            {
                await Task.WhenAny(Task.WhenAll(_runningTasks), Task.Delay(3000));
                _logger.LogInformation("Old services stopped");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while stopping services");
            }
            
            _currentCts.Dispose();
        }
        
        // Dispose old scope
        _scope?.Dispose();
        
        // Wait for cleanup
        _logger.LogInformation("Waiting for cleanup...");
        await Task.Delay(1000);
        
        // CREATE FRESH CHANNELS
        _logger.LogInformation("Creating fresh channels...");
        _loopbackSendChannel = Channel.CreateBounded<WsMessage>(
            new BoundedChannelOptions(capacity: 1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
        
        _micSendChannel = Channel.CreateBounded<WsMessage>(
            new BoundedChannelOptions(capacity: 1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
        
        _captionChannel = Channel.CreateBounded<CaptionMessage>(
            new BoundedChannelOptions(capacity: 1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
        
        // Start everything fresh with new languages
        _logger.LogInformation("Starting fresh services...");
        _currentCts = new CancellationTokenSource();
        _runningTasks.Clear();

        // Create fresh Soniox services with fresh channels
        var loopbackLogger = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<SonioxWsService>>();
        var micLogger = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<SonioxWsService>>();
        
        var loopbackSonioxWsService = new SonioxWsService(loopbackLogger, _sonioxHttpClient, _loopbackSendChannel, _captionChannel);
        var micSonioxWsService = new SonioxWsService(micLogger, _sonioxHttpClient, _micSendChannel, _captionChannel);

        // Set languages BEFORE starting services
        _logger.LogInformation($"Setting languages to: {sourceLanguage} → {targetLanguage}");
        loopbackSonioxWsService.SetLanguages(sourceLanguage, targetLanguage);
        micSonioxWsService.SetLanguages(sourceLanguage, targetLanguage);

        // Start Soniox WebSocket services
        _logger.LogInformation("Starting Loopback Soniox WS service...");
        _runningTasks.Add(Task.Run(() => loopbackSonioxWsService.Run(_currentCts.Token), _currentCts.Token));
        
        _logger.LogInformation("Starting Microphone Soniox WS service...");
        _runningTasks.Add(Task.Run(() => micSonioxWsService.Run(_currentCts.Token), _currentCts.Token));

        // Create fresh audio capture services
        var captureLogger = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<CaptureAudioService>>();
        
        var loopbackCaptureService = new CaptureAudioService(captureLogger, _loopbackSendChannel, loopbackSonioxWsService, ECaptureSourceType.Loopback);
        var micCaptureService = new CaptureAudioService(captureLogger, _micSendChannel, micSonioxWsService, ECaptureSourceType.Microphone);

        _logger.LogInformation("Starting Loopback audio capture...");
        _runningTasks.Add(Task.Run(() => loopbackCaptureService.StartCaptureAudio(_currentCts.Token), _currentCts.Token));
        
        _logger.LogInformation("Starting Microphone audio capture...");
        _runningTasks.Add(Task.Run(() => micCaptureService.StartCaptureAudio(_currentCts.Token), _currentCts.Token));

        // Create fresh caption service
        var captionLogger = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<CaptionService>>();
        var captionHub = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<IHubContext<CaptionHub, ICaptionClient>>();
        // Note: CaptionService uses loopbackSonioxWsService for now (can be changed to use both if needed)
        _captionService = new CaptionService(_captionChannel, captionLogger, loopbackSonioxWsService, captionHub);
        _runningTasks.Add(Task.Run(() => _captionService.SendingCaptionsTask(_currentCts.Token), _currentCts.Token));

        _logger.LogInformation("✅ All services restarted successfully with fresh channels!");
    }
    
    
    //samo stop restart start services only !!!  TODO:

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 INITIAL STARTUP - Starting all services...");
        
        // Use the same restart method for initial startup
        _ = Task.Run(async () => 
        {
            await Task.Delay(100); // Small delay to let DI container fully initialize
            await RestartAllServices("sl", "en"); // Default languages
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_captureAudioService is not null)
        {
            await _captureAudioService.StopCaptureAudio();
        }

        if (_sonioxWsService is not null)
        {
            await _sonioxWsService.Stop(cancellationToken);
        }

        _scope?.Dispose();
    }
}
