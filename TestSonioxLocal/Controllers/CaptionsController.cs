using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TestSonioxLocal.Hubs;
using TestSonioxLocal.Services.HttpClients;

namespace TestSonioxLocal.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CaptionsController : ControllerBase
{
    private readonly IHubContext<CaptionHub, ICaptionClient> _hubContext;
    private readonly ISonioxHttpClient _sonioxHttpClient;

    public CaptionsController(IHubContext<CaptionHub, ICaptionClient> hubContext, ISonioxHttpClient sonioxHttpClient)
    {
        _hubContext = hubContext;
        _sonioxHttpClient = sonioxHttpClient;
    }

    [HttpGet("api-key")]
    public async Task<IActionResult> GetApiKey()
    {
        try
        {
            string apiKey = await _sonioxHttpClient.GetSonioxTempApiKey(HttpContext.RequestAborted);
            return Ok(new { apiKey });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task SendCaption([FromQuery] string caption)
    {
        // OLD: Only sent caption text
        // await _hubContext.Clients.All.ReceiveCaption(caption);
        
        // NEW: Send with all required parameters (caption, isFinal, speaker, isTranslation)
        await _hubContext.Clients.All.ReceiveCaption(caption, true, null, false);
    }
}
