using Microsoft.AspNetCore.Mvc;
using TestSonioxLocal.Services;

namespace TestSonioxLocal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OBSStylingController : ControllerBase
{
    private readonly IOBSWebSocketService _obsWebSocketService;
    private readonly ILogger<OBSStylingController> _logger;

    public OBSStylingController(
        IOBSWebSocketService obsWebSocketService,
        ILogger<OBSStylingController> logger)
    {
        _obsWebSocketService = obsWebSocketService;
        _logger = logger;
    }

    [HttpPost("update-css")]
    public async Task<IActionResult> UpdateCSS([FromBody] UpdateCSSRequest request)
    {
        try
        {
            _logger.LogInformation($"Updating CSS for source: {request.SourceName}");

            var success = await _obsWebSocketService.UpdateBrowserSourceCSS(
                request.SourceName, 
                request.CSS);

            if (success)
            {
                return Ok(new { success = true, message = "CSS updated successfully" });
            }
            else
            {
                return BadRequest(new { success = false, message = "Failed to update CSS. Check OBS WebSocket connection." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CSS");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("connection-status")]
    public async Task<IActionResult> GetConnectionStatus()
    {
        try
        {
            var isConnected = await _obsWebSocketService.IsConnected();
            return Ok(new { connected = isConnected });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking connection status");
            return StatusCode(500, new { connected = false, error = ex.Message });
        }
    }
}

public class UpdateCSSRequest
{
    public string SourceName { get; set; } = "";
    public string CSS { get; set; } = "";
}
