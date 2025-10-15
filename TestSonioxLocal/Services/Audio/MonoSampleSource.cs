using CSCore;

namespace TestSonioxLocal.Services.Audio;

public class MonoSampleSource : ISampleSource
{
    private readonly ISampleSource _source;
    private readonly int _channelIndex;

    public MonoSampleSource(ISampleSource source, int channelIndex)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        if (channelIndex < 0 || channelIndex >= source.WaveFormat.Channels)
            throw new ArgumentOutOfRangeException(nameof(channelIndex));

        _channelIndex = channelIndex;
    }

    public WaveFormat WaveFormat => new WaveFormat(_source.WaveFormat.SampleRate, _source.WaveFormat.BitsPerSample, 1);

    public bool CanSeek => _source.CanSeek;

    // Position in mono samples
    public long Position
    {
        get => _source.Position / _source.WaveFormat.Channels;
        set
        {
            if (!CanSeek) throw new NotSupportedException();
            _source.Position = value * _source.WaveFormat.Channels;
        }
    }

    public long Length => _source.Length / _source.WaveFormat.Channels;

    public int Read(float[] buffer, int offset, int count)
    {
        // Number of mono samples to read
        int samplesToRead = count;
        float[] tempBuffer = new float[samplesToRead * _source.WaveFormat.Channels];
        int read = _source.Read(tempBuffer, 0, tempBuffer.Length);

        int monoSamples = read / _source.WaveFormat.Channels;
        for (int i = 0; i < monoSamples; i++)
        {
            buffer[offset + i] = tempBuffer[i * _source.WaveFormat.Channels + _channelIndex];
        }

        return monoSamples;
    }

    public void Dispose()
    {
        _source.Dispose();
    }
}
