using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TestSonioxLocal.Services;

public interface IOBSWebSocketService
{
    Task<bool> UpdateBrowserSourceCSS(string sourceName, string css);
    Task<bool> IsConnected();
}

public class OBSWebSocketService : IOBSWebSocketService
{
    private readonly HttpClient _httpClient;
    private readonly OBSWebSocketOptions _options;
    private readonly ILogger<OBSWebSocketService> _logger;
    private string? _authToken;

    public OBSWebSocketService(
        HttpClient httpClient, 
        IOptions<OBSWebSocketOptions> options,
        ILogger<OBSWebSocketService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> IsConnected()
    {
        try
        {
            var response = await _httpClient.GetAsync($"http://{_options.Host}:{_options.Port}/api/v1/status");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OBS WebSocket not available");
            return false;
        }
    }

    public async Task<bool> UpdateBrowserSourceCSS(string sourceName, string css)
    {
        try
        {
            // First, check if we're connected
            if (!await IsConnected())
            {
                _logger.LogWarning("OBS WebSocket not connected - styling changes will not be applied");
                return false;
            }

            // Get authentication token if we don't have one
            if (string.IsNullOrEmpty(_authToken))
            {
                _authToken = await GetAuthToken();
                if (string.IsNullOrEmpty(_authToken))
                {
                    _logger.LogError("Failed to get OBS WebSocket authentication token");
                    return false;
                }
            }

            // Prepare the request to update browser source settings
            var request = new
            {
                sourceName = sourceName,
                sourceSettings = new
                {
                    custom_css = css
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

            var response = await _httpClient.PostAsync(
                $"http://{_options.Host}:{_options.Port}/api/v1/sources/settings", 
                content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Successfully updated CSS for source: {sourceName}");
                return true;
            }
            else
            {
                _logger.LogError($"Failed to update CSS for source {sourceName}: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating CSS for source {sourceName}");
            return false;
        }
    }

    private async Task<string?> GetAuthToken()
    {
        try
        {
            var authRequest = new
            {
                challenge = _options.Password
            };

            var json = JsonSerializer.Serialize(authRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"http://{_options.Host}:{_options.Port}/api/v1/auth", 
                content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent);
                return authResponse?.token;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate with OBS WebSocket");
        }

        return null;
    }
}

public class OBSWebSocketOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 4455;
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string? token { get; set; }
}
