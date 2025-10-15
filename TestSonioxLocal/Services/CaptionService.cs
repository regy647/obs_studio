using Microsoft.AspNetCore.SignalR;
using System.Threading.Channels;
using TestSonioxLocal.Hubs;
using TestSonioxLocal.Models;

namespace TestSonioxLocal.Services;

public interface ICaptionService
{
    Task SendingCaptionsTask(CancellationToken cancellationToken);
}

public class CaptionService : ICaptionService
{
    private readonly Channel<CaptionMessage> _captionChannel;
    private readonly ILogger<CaptionService> _logger;
    private readonly ISonioxWsService _sonioxWsService;
    private readonly IHubContext<CaptionHub, ICaptionClient> _hubContext;

    public CaptionService(
        Channel<CaptionMessage> captionChannel,
        ILogger<CaptionService> logger,
        ISonioxWsService sonioxWsService,
        IHubContext<CaptionHub, ICaptionClient> hubContext)
    {
        _captionChannel = captionChannel;
        _logger = logger;
        _sonioxWsService = sonioxWsService;
        _hubContext = hubContext;
    }

    public async Task SendingCaptionsTask(CancellationToken cancellationToken)
    {
        await _sonioxWsService.Ready;
                
        await foreach (var caption in _captionChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                // OLD: Only sent caption text
                // await _hubContext.Clients.All.ReceiveCaption(caption.Text);
                
                // NEW: Send caption text, isFinal flag, speaker info, and isTranslation flag
                _logger.LogInformation($"CaptionService sending: Text='{caption.Text}', IsFinal={caption.IsFinal}, Speaker='{caption.Speaker}', IsTranslation={caption.IsTranslation}");
                await _hubContext.Clients.All.ReceiveCaption(caption.Text, caption.IsFinal, caption.Speaker, caption.IsTranslation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending caption");
            }
        }
    }
}
