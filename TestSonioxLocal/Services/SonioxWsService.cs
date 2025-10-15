using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using TestSonioxLocal.Models;
using TestSonioxLocal.Services.HttpClients;

namespace TestSonioxLocal.Services;

public interface ISonioxWsService
{
    Task Run(CancellationToken stopToken);
    Task Stop(CancellationToken stopToken);
    Task Ready { get; }
    Task UpdateLanguageSettings(string sourceLanguage, string targetLanguage);
    void SetLanguages(string sourceLanguage, string targetLanguage);
}

public interface ILoopbackSonioxWsService : ISonioxWsService { }

public interface IMicSonioxWsService : ISonioxWsService { }

public class SonioxWsService : ILoopbackSonioxWsService, IMicSonioxWsService
{
    private readonly ILogger<SonioxWsService> _logger;
    private readonly ISonioxHttpClient _sonioxHttpClient;
    private readonly Channel<WsMessage> _sendChannel;
    private readonly Channel<CaptionMessage> _captionChannel;
    private TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    
    // Language settings - default to Slovenian and English
    private string _sourceLanguage = "sl";
    private string _targetLanguage = "en";

    // Session management for reconnection
    private CancellationTokenSource? _sessionCts;
    private Task? _sessionTask;

    private ClientWebSocket? _ws;
    private Task? _senderTask = null;
    private Task? _sendingCaptionsTask = null;
    private Task? _receivingTask = null;
    private Task? _keepAliveTask = null;

    public SonioxWsService(
        ILogger<SonioxWsService> logger,
        ISonioxHttpClient sonioxHttpClient,
        Channel<WsMessage> sendChannel,
        Channel<CaptionMessage> captionChannel)
    {
        _logger = logger;
        _sonioxHttpClient = sonioxHttpClient;
        _sendChannel = sendChannel;
        _captionChannel = captionChannel;
    }

    public Task Ready => _readyTcs.Task;
    
    public void SetLanguages(string sourceLanguage, string targetLanguage)
    {
        _logger.LogInformation($"Setting languages: {sourceLanguage} → {targetLanguage}");
        _sourceLanguage = sourceLanguage;
        _targetLanguage = targetLanguage;
    }
    
    public async Task UpdateLanguageSettings(string sourceLanguage, string targetLanguage)
    {
        _logger.LogInformation($"[LANG UPDATE] Updating language settings: {sourceLanguage} → {targetLanguage}");
        
        // Update language settings
        _sourceLanguage = sourceLanguage;
        _targetLanguage = targetLanguage;
        
        // Cancel current session if running
        if (_sessionCts != null)
        {
            _logger.LogInformation($"[LANG UPDATE] Session CTS exists. IsCancellationRequested: {_sessionCts.IsCancellationRequested}");
            
            if (!_sessionCts.IsCancellationRequested)
            {
                _logger.LogInformation("[LANG UPDATE] Cancelling current session...");
                _sessionCts.Cancel();
            
                
                // Wait for current session to end
                if (_sessionTask != null)
                {
                    _logger.LogInformation("[LANG UPDATE] Waiting for current session to end...");
                    try
                    {
                        await Task.WhenAny(_sessionTask, Task.Delay(5000)); // Wait max 5 seconds
                        _logger.LogInformation("[LANG UPDATE] Session ended or timeout reached");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[LANG UPDATE] Error while waiting for session to end");
                    }
                }
                
                _sessionCts.Dispose();
                _logger.LogInformation("[LANG UPDATE] Old session disposed");
            }
        }
        else
        {
            _logger.LogInformation("[LANG UPDATE] No existing session to cancel");
        }
        
        // Start new session with new language settings
        _logger.LogInformation($"Starting new session with languages: {sourceLanguage} → {targetLanguage}");
        
        // Reset ready task for new session
        _readyTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        _sessionCts = new CancellationTokenSource();
        
        // Combine with the original cancellation token (from Run method)
        // For now, just start a new session - the Run method will handle the main cancellation token
        _sessionTask = RunSession(_sessionCts.Token);
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        // Initialize and start the first session
        _sessionCts = new CancellationTokenSource();
        _sessionTask = RunSession(_sessionCts.Token);
        
        // Wait for the main cancellation token (app shutdown)
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task RunSession(CancellationToken sessionToken)
    {
        _ws = new ClientWebSocket();

        _logger.LogInformation("Connecting...");
        await _ws.ConnectAsync(new Uri("wss://stt-rt.soniox.com/transcribe-websocket"), sessionToken);
        _logger.LogInformation("Connected!");

        // Sender loop (only this calls ws.SendAsync)
        _senderTask = Task.Run(() => SenderTask(_ws, sessionToken), sessionToken);

        // Send init message to Soniox
        string sonioxTempApiKey = await _sonioxHttpClient.GetSonioxTempApiKey(sessionToken);

        // OLD: Init message without speaker identification and translation
        // var initMessage = new
        // {
        //     api_key = sonioxTempApiKey,
        //     audio_format = "pcm_s16le",
        //     sample_rate = 16000,
        //     num_channels = 1,
        //     model = "stt-rt-preview-v2",
        //     language_hints = new List<string>() { "sl" },
        //     enable_endpoint_detection = true
        // };

        // NEW: Init message WITH speaker diarization AND two-way translation enabled
        _logger.LogInformation($"Initializing Soniox WebSocket with languages: {_sourceLanguage} → {_targetLanguage}");
        
        var initMessage = new
        {
            api_key = sonioxTempApiKey,
            audio_format = "pcm_s16le",
            sample_rate = 16000,
            num_channels = 1,
            model = "stt-rt-preview-v2",
            // language_hints removed - not needed with two-way translation
            enable_endpoint_detection = true,
            enable_speaker_diarization = true,  // Enable speaker diarization (correct parameter name)
            translation = new  // Enable two-way translation - dynamic languages
            {
                type = "two_way",
                language_a = _sourceLanguage,  // Use current source language setting
                language_b = _targetLanguage   // Use current target language setting
            }
        };
        
        // Log the full init message for debugging
        _logger.LogInformation($"Sending init message: {JsonSerializer.Serialize(initMessage)}");

        _sendChannel.Writer.TryWrite(new WsMessage(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(initMessage)),
            WebSocketMessageType.Text,
            true,
            true));

        await _readyTcs.Task; // wait until really sent

        // Start a task to listen for responses
        _receivingTask = Task.Run(() => ReceivingTask(_ws, sessionToken), sessionToken);

        // Start keep-alive task
        _keepAliveTask = Task.Run(() => KeepAliveTask(_ws, sessionToken), sessionToken);
        
        // Wait for session to be cancelled or tasks to complete
        try
        {
            await Task.WhenAll(_senderTask, _receivingTask, _keepAliveTask);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Session cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session error");
        }
        finally
        {
            // Clean up WebSocket
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                try
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing WebSocket");
                }
            }
            
            _ws?.Dispose();
            _logger.LogInformation("Session ended");
        }
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        // Send final empty frame with EndOfMessage = true
        _sendChannel.Writer.TryWrite(new WsMessage(ArraySegment<byte>.Empty, WebSocketMessageType.Binary, true, false));
        _logger.LogInformation("Finished sending audio.");

        // Shutdown channels
        _sendChannel.Writer.Complete();
        if (_senderTask is not null)
        {
            await _senderTask;
        }

        // Dispose websocket
        if (_ws is not null)
        {
            _ws.Dispose();
            _ws = null;
        }

        _captionChannel.Writer.Complete();
        if (_sendingCaptionsTask is not null)
        {
            await _sendingCaptionsTask;
        }

        // Wait a bit to receive responses
        await Task.Delay(3000, cancellationToken);

        // Ensure receiving task stop
        if (_receivingTask is not null)
        {
            await _receivingTask;
        }

        // Ensure keep alive task stop
        if (_keepAliveTask is not null)
        {
            await _keepAliveTask;
        }
    }

    private async Task SenderTask(ClientWebSocket ws, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var msg in _sendChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    // Check if WebSocket is still open
                    if (ws.State != WebSocketState.Open)
                    {
                        _logger.LogInformation("WebSocket closed, stopping sender task");
                        break;
                    }
                    
                    await ws.SendAsync(msg.Payload, msg.MessageType, msg.EndOfMessage, cancellationToken);

                    if (msg.IsInit)
                    {
                        _readyTcs.TrySetResult();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Sender task cancelled");
                    break;
                }
                catch (WebSocketException ex) when (ws.State != WebSocketState.Open)
                {
                    _logger.LogInformation("WebSocket closed during send");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Send error");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Sender task cancelled via channel");
        }
    }

    private async Task ReceivingTask(ClientWebSocket ws, CancellationToken cancellationToken)
    {
        var receiveBuffer = new byte[4096];
        // OLD: Only tracked final tokens
        // List<string> finalTokens = new();
        
        // NEW: Track both final and non-final tokens with speaker info
        List<Token> allTokens = new();
        string? currentSpeaker = null;

        while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Server closed connection.");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                    using var doc = JsonDocument.Parse(message);
                    var tokens = doc.RootElement.GetProperty("tokens");

                    if (tokens.GetArrayLength() > 0)
                    {
                        List<Token> tokenObjs = tokens.Deserialize<List<Token>>()?.ToList()
                            ?? throw new InvalidOperationException("Cannot deserialize to list of tokens.");

                        // OLD: Only processed final tokens
                        // List<Token> tempTokenObjs = tokenObjs.Where(x => x.IsFinal).ToList();
                        // if (tempTokenObjs.Count > 0)
                        // {
                        //     finalTokens.AddRange(tempTokenObjs.Select(x => x.Text));
                        //     if (tempTokenObjs.Last().Text == "<end>")
                        //     {
                        //         string caption = string.Join("", finalTokens).Replace("<end>", "");
                        //         _logger.LogInformation(caption);
                        //         await _captionChannel.Writer.WriteAsync(new CaptionMessage(caption));
                        //         finalTokens = new();
                        //     }
                        // }

                        // NEW: Process ALL tokens (both final and non-final) and separate transcription from translation
                        foreach (var token in tokenObjs)
                        {
                            // Log the raw token for debugging
                            _logger.LogInformation($"Raw token: Text='{token.Text}', Speaker='{token.Speaker}', IsFinal={token.IsFinal}, TranslationStatus='{token.TranslationStatus}'");
                            
                            // Update current speaker if token has speaker info
                            if (!string.IsNullOrEmpty(token.Speaker))
                            {
                                currentSpeaker = token.Speaker;
                                _logger.LogInformation($"Speaker updated to: {currentSpeaker}");
                            }

                            // Skip endpoint markers
                            if (token.Text == "<end>")
                            {
                                _logger.LogInformation("Endpoint marker received");
                                continue;
                            }

                            // Determine if this is a transcription or translation token
                            // translation_status: "original" = transcription, "translation" = translation, "none" = transcription only (no translation)
                            bool isTranslation = token.TranslationStatus == "translation";
                            
                            string tokenType = isTranslation ? "TRANSLATION" : "TRANSCRIPTION";
                            _logger.LogInformation($"[{tokenType}] [{(token.IsFinal ? "FINAL" : "PARTIAL")}] Speaker: '{currentSpeaker ?? token.Speaker}' -> Text: '{token.Text}'");

                            // Send token to frontend with all information: text, isFinal, speaker, isTranslation
                            await _captionChannel.Writer.WriteAsync(
                                new CaptionMessage(
                                    token.Text, 
                                    token.IsFinal, 
                                    currentSpeaker ?? token.Speaker, 
                                    isTranslation));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Receiving task cancelled");
                break;
            }
            catch (WebSocketException ex) when (ws.State != WebSocketState.Open || cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("WebSocket closed or session cancelled during receive");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                break;
            }
        }
        
        _logger.LogInformation("Receiving task ended");
    }

    private async Task KeepAliveTask(ClientWebSocket ws, CancellationToken cancellationToken)
    {
        try
        {
            while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(10000, cancellationToken);

                try
                {
                    // Check if WebSocket is still open
                    if (ws.State != WebSocketState.Open)
                    {
                        _logger.LogInformation("WebSocket closed, stopping keep-alive task");
                        break;
                    }
                    
                    var keepAliveMessage = new
                    {
                        type = "keepalive"
                    };

                    _sendChannel.Writer.TryWrite(new WsMessage(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(keepAliveMessage))),
                        WebSocketMessageType.Text,
                        true,
                        false));

                    _logger.LogInformation("Keep-alive sent.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Keep-alive task cancelled");
        }
    }
}

