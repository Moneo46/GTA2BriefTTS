using System.Diagnostics;
using System.Text.Json;
using Frida;

namespace GTA2BriefTTS.User;

public sealed class InjectSession : IDisposable
{
    private readonly Action<string> _log;
    private readonly Action? _onFinished;
    private readonly UserSettings _settings;
    private Thread? _worker;
    private volatile bool _stop;

    private Mp3QueuePlayer? _player;
    private DeviceManager? _deviceManager;
    private Session? _session;
    private Script? _script;

    public bool IsRunning => _worker is { IsAlive: true };

    public InjectSession(UserSettings settings, Action<string> log, Action? onFinished = null)
    {
        _settings = settings;
        _log = log;
        _onFinished = onFinished;
    }

    public void Start()
    {
        if (IsRunning)
            return;
        _stop = false;
        UserConfigStore.Save(_settings);
        _worker = new Thread(Run) { IsBackground = true, Name = "brief-tts-session" };
        _worker.Start();
    }

    public void Stop()
    {
        _stop = true;
        _player?.StopPlayback();
    }

    public void SetTtsVolume(float volume)
    {
        _player?.SetVolume(volume);
    }

    public void SetBriefPlaybackSpeed(float speed)
    {
        _player?.SetPlaybackSpeed(speed);
    }

    private void Run()
    {
        try
        {
            AppPaths.EnsureCwd();
            using var cfgDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppPaths.AppRoot, "config.json")));
            var cfg = cfgDoc.RootElement;
            var gameRoot = AppPaths.ResolveGameFolder(_settings.GameRoot);

            if (GameLanguage.IsProcessRunning(_settings.ProcessName))
                _log(UiStrings.GameAlreadyRunning);

            GameLanguage.Apply(_settings.GameLanguage);
            GameSoundSettings.Write(_settings.GameSfxVolume, _settings.GameMusicVolume);

            var gxtPath = LangConfig.GxtPath(cfg, gameRoot, _settings.TtsLanguage);
            if (!File.Exists(gxtPath))
            {
                _log(UiStrings.GxtMissing(gxtPath));
                _log(UiStrings.SetGameFolderHint);
                return;
            }

            var briefs = GxtReader.LoadBriefLines(gxtPath);
            _log(UiStrings.BriefsLoaded(briefs.Count, _settings.TtsLanguage));

            _player = new Mp3QueuePlayer(_settings.TtsVolume, _settings.BriefPlaybackSpeed);
            var speaker = new BriefSpeaker(_settings.TtsLanguage, _player);

            GameLanguage.Apply(_settings.GameLanguage);

            TryLaunchGame(gameRoot);

            _log(UiStrings.WaitingForGame(_settings.ProcessName));
            _deviceManager = new DeviceManager();
            var local = _deviceManager.EnumerateDevices().First(d => d.Type == DeviceType.Local);

            Frida.Process? game = null;
            while (game == null && !_stop)
            {
                game = local.EnumerateProcesses().FirstOrDefault(p =>
                    string.Equals(p.Name, _settings.ProcessName, StringComparison.OrdinalIgnoreCase));
                if (game == null)
                    Thread.Sleep(500);
            }

            if (_stop || game == null)
            {
                _log(UiStrings.Cancelled);
                return;
            }

            _log(UiStrings.GameFound);
            _session = local.Attach(game.Pid);
            var agentSource = AddressConfig.BuildAgentSource(cfg, debug: false);
            var state = new InjectState();

            _script = _session.CreateScript(agentSource);
            _script.Message += (_, ev) =>
                FridaMessageHandler.OnMessage(ev, speaker, briefs, state, _log, showDebug: false);
            _script.Load();

            while (!_stop && GameLanguage.IsProcessRunning(_settings.ProcessName))
                Thread.Sleep(500);

            if (!_stop)
                _log(UiStrings.GameExited);
            else
                _log(UiStrings.SessionStopped);
        }
        catch (Exception ex)
        {
            _log(UiStrings.Error(ex.Message));
        }
        finally
        {
            try
            {
                _script?.Dispose();
                _session?.Detach();
                _session?.Dispose();
            }
            catch
            {
                /* ignore */
            }

            _script = null;
            _session = null;
            _deviceManager?.Dispose();
            _deviceManager = null;
            _player?.Dispose();
            _player = null;
            _onFinished?.Invoke();
        }
    }

    private void TryLaunchGame(string gameRoot)
    {
        if (GameLanguage.IsProcessRunning(_settings.ProcessName))
            return;
        var exe = Path.Combine(gameRoot, _settings.ProcessName);
        if (!File.Exists(exe))
            return;
        try
        {
            _log(UiStrings.LaunchingGame);
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = gameRoot,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _log(UiStrings.LaunchFailed(ex.Message));
        }
    }

    public void Dispose()
    {
        Stop();
        _worker?.Join(TimeSpan.FromSeconds(8));
    }
}
