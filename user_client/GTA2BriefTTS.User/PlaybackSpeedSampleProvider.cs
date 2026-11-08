using NAudio.Wave;

namespace GTA2BriefTTS.User;

/// <summary>Varispeed playback (pitch follows speed).</summary>
internal sealed class PlaybackSpeedSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float[] _chunk = new float[4096];
    private long _chunkStart;
    private int _chunkLen;
    private long _outIndex;

    public PlaybackSpeedSampleProvider(ISampleProvider source)
    {
        _source = source;
        WaveFormat = source.WaveFormat;
        _chunkStart = 0;
        _chunkLen = 0;
    }

    public WaveFormat WaveFormat { get; }

    public float Speed { get; set; } = 1f;

    public int Read(float[] buffer, int offset, int count)
    {
        var speed = Math.Clamp(Speed, 0.25f, 3f);
        for (var i = 0; i < count; i++)
        {
            var srcPos = _outIndex * speed;
            buffer[offset + i] = SampleAt(srcPos);
            _outIndex++;
        }

        return count;
    }

    private float SampleAt(double position)
    {
        var idx = (long)Math.Floor(position);
        var frac = (float)(position - idx);
        while (idx >= _chunkStart + _chunkLen)
        {
            if (!FillNextChunk())
                return 0f;
        }

        if (idx < _chunkStart)
            return 0f;
        var local = (int)(idx - _chunkStart);
        var a = _chunk[local];
        if (local + 1 >= _chunkLen)
            return a;
        var b = _chunk[local + 1];
        return a + (b - a) * frac;
    }

    private bool FillNextChunk()
    {
        _chunkStart += _chunkLen;
        _chunkLen = _source.Read(_chunk, 0, _chunk.Length);
        return _chunkLen > 0;
    }
}
