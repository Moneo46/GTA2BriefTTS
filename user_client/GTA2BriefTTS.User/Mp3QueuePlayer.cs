using System.Collections.Concurrent;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace GTA2BriefTTS.User;

public sealed class Mp3QueuePlayer : IDisposable
{
    private readonly BlockingCollection<string> _queue = new();
    private readonly Thread _thread;
    private readonly object _outLock = new();
    private float _volume;
    private float _playbackSpeed = 1f;
    private WaveOutEvent? _out;
    private PlaybackSpeedSampleProvider? _speedProvider;
    private bool _disposed;

    public Mp3QueuePlayer(float volume, float playbackSpeed = 1f)
    {
        _volume = Math.Clamp(volume, 0f, 1f);
        _playbackSpeed = Math.Clamp(playbackSpeed, 0.25f, 3f);
        _thread = new Thread(PlayLoop) { IsBackground = true, Name = "mp3-play" };
        _thread.Start();
    }

    public void Enqueue(string mp3Path, bool blockUntilDone = false)
    {
        if (_disposed)
            return;
        if (!blockUntilDone)
        {
            _queue.Add(mp3Path);
            return;
        }

        using var gate = new ManualResetEventSlim(false);
        _queue.Add(mp3Path);
        // For _ready fire-and-forget is fine — same as Python block=False
        _ = gate;
    }

    public void SetVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0f, 1f);
        lock (_outLock)
        {
            if (_out != null)
                _out.Volume = _volume;
        }
    }

    public void SetPlaybackSpeed(float speed)
    {
        _playbackSpeed = Math.Clamp(speed, 0.25f, 3f);
        lock (_outLock)
        {
            if (_speedProvider != null)
                _speedProvider.Speed = _playbackSpeed;
        }
    }

    /// <summary>Stop the clip that is playing now and drop pending briefs.</summary>
    public void StopPlayback()
    {
        lock (_outLock)
        {
            try
            {
                _out?.Stop();
            }
            catch
            {
                /* ignore */
            }
        }

        while (_queue.TryTake(out _)) { }
    }

    public void PlayReadyOrSkip(TtsCache cache, string voiceFolder, string readyKey = "_ready")
    {
        try
        {
            var path = cache.PathFor(voiceFolder, readyKey);
            if (path != null)
                _queue.Add(path);
        }
        catch
        {
            /* optional */
        }
    }

    private void PlayLoop()
    {
        try
        {
            foreach (var path in _queue.GetConsumingEnumerable())
            {
                try
                {
                    using var reader = new Mp3FileReader(path);
                    var speed = _playbackSpeed;
                    var speedProvider = new PlaybackSpeedSampleProvider(reader.ToSampleProvider())
                    {
                        Speed = speed,
                    };
                    var waveProvider = new SampleToWaveProvider16(speedProvider);
                    WaveOutEvent player;
                    lock (_outLock)
                    {
                        _out?.Dispose();
                        _speedProvider = speedProvider;
                        player = new WaveOutEvent();
                        _out = player;
                        player.Volume = _volume;
                        player.Init(waveProvider);
                        player.Play();
                    }

                    while (true)
                    {
                        PlaybackState state;
                        lock (_outLock)
                        {
                            state = _out?.PlaybackState ?? PlaybackState.Stopped;
                        }

                        if (state != PlaybackState.Playing)
                            break;
                        Thread.Sleep(10);
                    }

                    lock (_outLock)
                        _speedProvider = null;
                }
                catch (Exception)
                {
                }
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _queue.CompleteAdding();
        _thread.Join(TimeSpan.FromSeconds(5));
        lock (_outLock)
            _out?.Dispose();
    }
}
