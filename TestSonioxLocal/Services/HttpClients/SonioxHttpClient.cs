using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace TestSonioxLocal.Services.HttpClients;

public interface ISonioxHttpClient
{
    Task<string> GetSonioxTempApiKey(CancellationToken cancellationToken);
}

public class SonioxHttpClient : ISonioxHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SonioxHttpClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GetSonioxTempApiKey(CancellationToken cancellationToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();

        var apiKey = _configuration["SonioxApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_SONIOX_API_KEY_HERE")
        {
            throw new InvalidOperationException("Soniox API key is not configured. Please set 'SonioxApiKey' in appsettings.json");
        }

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var jsonObject = new
        {
            usage_type = "transcribe_websocket",
            expires_in_seconds = 600
        };

        var content = new StringContent(JsonSerializer.Serialize(jsonObject), Encoding.UTF8, "application/json");

        var responseMessage = await _httpClient.PostAsync(
            "https://api.soniox.com/v1/auth/temporary-api-key",
            content,
            cancellationToken);

        string responseContent = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(responseContent);

        return doc.RootElement.GetProperty("api_key").GetString()
            ?? throw new InvalidOperationException("Cannot extract api_key from response.");
    }
}
