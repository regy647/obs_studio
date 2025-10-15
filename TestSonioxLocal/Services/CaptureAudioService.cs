using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;
using System.Net.WebSockets;
using System.Threading.Channels;
using TestSonioxLocal.Models;
using TestSonioxLocal.Models.Enums;
using TestSonioxLocal.Services.Audio;

namespace TestSonioxLocal.Services;

public interface ICaptureAudioService
{
    Task StartCaptureAudio(CancellationToken cancellationToken);
    Task StopCaptureAudio();
}

public class CaptureAudioService : ICaptureAudioService
{
    private readonly ILogger<CaptureAudioService> _logger;
    private readonly Channel<WsMessage> _sendChannel;
    private readonly ISonioxWsService _sonioxWsService;
    private readonly ECaptureSourceType _captureSourceType;

    WasapiLoopbackCapture? _loopbackCapture = null;
    WasapiCapture? _micCapture = null;
    
    private int _silenceCounter = 0; // Track silence packets for logging

    public CaptureAudioService(
        ILogger<CaptureAudioService> logger, 
        Channel<WsMessage> sendChannel,
        ISonioxWsService sonioxWsService,
        ECaptureSourceType captureSourceType)
    {
        _logger = logger;
        _sendChannel = sendChannel;
        _sonioxWsService = sonioxWsService;
        _captureSourceType = captureSourceType;
    }

    public async Task StartCaptureAudio(CancellationToken cancellationToken)
    {
        //
        await _sonioxWsService.Ready;
        
        using var deviceEnumerator = new MMDeviceEnumerator();

        byte[] buffer = new byte[4096];

        if (_captureSourceType == ECaptureSourceType.Loopback)
        {
            var loopbackDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            _loopbackCapture = new WasapiLoopbackCapture() { Device = loopbackDevice };
            _loopbackCapture.Initialize();

            var loopbackSoundInSource = new SoundInSource(_loopbackCapture);
            var loopbackSampleSource = loopbackSoundInSource.ToSampleSource();
            var loopbackResampled = loopbackSampleSource.ChangeSampleRate(16000);
            var loopbackMono = loopbackResampled.ToMono();
            var loopbackFinalSource = loopbackMono.ToWaveSource(16);

            loopbackSoundInSource.DataAvailable += (s, e) =>
            {
                try
                {
                    int read = loopbackFinalSource.Read(buffer, 0, e.ByteCount);
                    if (read > 0)
                    {
                        // Calculate audio level for debugging
                        short maxAmplitude = 0;
                        for (int i = 0; i < read; i += 2)
                        {
                            if (i + 1 < read)
                            {
                                short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                                if (Math.Abs(sample) > Math.Abs(maxAmplitude))
                                    maxAmplitude = sample;
                            }
                        }
                        
                        // Log ALL audio activity for debugging
                        if (Math.Abs(maxAmplitude) > 100)
                        {
                            _logger.LogInformation($"🔊 AUDIO DETECTED: {read} bytes, max amplitude: {maxAmplitude}");
                            _silenceCounter = 0; // Reset silence counter when audio detected
                        }
                        else
                        {
                            // Log silence every 100 packets to avoid spam
                            _silenceCounter++;
                            if (_silenceCounter % 100 == 0)
                            {
                                _logger.LogWarning($"⚠️ SILENCE: {read} bytes captured, but max amplitude is only {maxAmplitude} (threshold: 100)");
                            }
                        }
                        
                        var segment = new ArraySegment<byte>(buffer, 0, read);
                        _sendChannel.Writer.TryWrite(new WsMessage(segment, WebSocketMessageType.Binary, true, false));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            };

            _loopbackCapture.Start();
        }
        else if (_captureSourceType == ECaptureSourceType.Microphone) {
            var deviceCollection = deviceEnumerator.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active);
            var micDevice = deviceCollection.FirstOrDefault();

            _micCapture = new WasapiCapture() { Device = micDevice };
            _micCapture.Initialize();

            var micSoundInSource = new SoundInSource(_micCapture);
            var micSampleSource = micSoundInSource.ToSampleSource();
            var micResampled = micSampleSource.ChangeSampleRate(16000);
            var micMono = new MonoSampleSource(micResampled, 0);
            var micFinalSource = micMono.ToWaveSource(16);

            micSoundInSource.DataAvailable += (s, e) =>
            {
                try
                {
                    int read = micFinalSource.Read(buffer, 0, e.ByteCount);
                    if (read > 0)
                    {
                        var segment = new ArraySegment<byte>(buffer, 0, read);
                        _sendChannel.Writer.TryWrite(new WsMessage(segment, WebSocketMessageType.Binary, true, false));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            };

            _micCapture.Start();
        }        

        _logger.LogInformation("Streaming audio... press Enter to stop.");
    }

    public Task StopCaptureAudio()
    {
        if (_loopbackCapture is not null)
        {
            _logger.LogInformation("Stopping capture now...");
            _loopbackCapture.Stop();
            _loopbackCapture.Dispose();
            _loopbackCapture = null;
        }

        if (_micCapture is not null)
        {
            _logger.LogInformation("Stopping capture now...");
            _micCapture.Stop();
            _micCapture.Dispose();
            _micCapture = null;
        }

        return Task.CompletedTask;
    }
}

