namespace GTA2BriefTTS.User;

public sealed class MainForm : Form
{
    private readonly Label _labelGame = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label _labelSubs = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label _labelTts = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label _labelTtsVol = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label _labelBriefSpeed = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label _labelSfx = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label _labelMusic = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label _logCaption = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        Padding = new Padding(10, 6, 0, 0),
    };

    private readonly TextBox _gameRoot = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _subtitleLang = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly ComboBox _ttsLang = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly TrackBar _ttsVol = new() { Minimum = 0, Maximum = 100, TickFrequency = 10, Width = 200 };
    private readonly Label _ttsVolLabel = new() { AutoSize = true };
    private readonly TrackBar _briefSpeed = new() { Minimum = 50, Maximum = 200, TickFrequency = 15, Width = 200, Value = 100 };
    private readonly Label _briefSpeedLabel = new() { AutoSize = true };
    private readonly TrackBar _sfxVol = new() { Minimum = 0, Maximum = 127, TickFrequency = 16, Width = 200 };
    private readonly Label _sfxVolLabel = new() { AutoSize = true };
    private readonly TrackBar _musicVol = new() { Minimum = 0, Maximum = 127, TickFrequency = 16, Width = 200 };
    private readonly Label _musicVolLabel = new() { AutoSize = true };
    private readonly Button _browse = new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
    private readonly Button _save = new() { AutoSize = true };
    private readonly Button _start = new() { AutoSize = true };
    private readonly Button _stop = new() { AutoSize = true, Enabled = false };
    private readonly TextBox _log = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        Font = new Font(FontFamily.GenericMonospace, 9f),
        BackColor = SystemColors.Window,
    };

    private UserSettings _settings = new();
    private InjectSession? _session;
    private readonly List<string> _hudLangCodes = new();

    public MainForm()
    {
        Text = UiStrings.WindowTitle;
        MinimumSize = new Size(560, 440);
        Size = new Size(620, 500);
        StartPosition = FormStartPosition.CenterScreen;
        try
        {
            var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (exeIcon != null)
                Icon = exeIcon;
        }
        catch
        {
            /* optional */
        }

        ApplyLocalizedCaptions();

        _browse.Click += OnBrowse;
        _save.Click += (_, _) => SaveFromUi(logMsg: true);
        _start.Click += OnStart;
        _stop.Click += OnStop;
        _ttsVol.ValueChanged += (_, _) => UpdateVolLabels();
        _briefSpeed.ValueChanged += (_, _) => UpdateVolLabels();
        _sfxVol.ValueChanged += (_, _) => UpdateVolLabels();
        _musicVol.ValueChanged += (_, _) => UpdateVolLabels();
        _gameRoot.Leave += (_, _) => RefreshHudLanguages();
        FormClosing += (_, _) =>
        {
            _session?.Dispose();
            SaveFromUi(logMsg: false);
        };

        var gamePathRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            Margin = new Padding(0),
        };
        gamePathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        gamePathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        gamePathRow.Controls.Add(_gameRoot, 0, 0);
        gamePathRow.Controls.Add(_browse, 1, 0);

        var settingsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            ColumnCount = 2,
        };
        settingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        void Row(Label label, Control value, int row)
        {
            settingsPanel.Controls.Add(label, 0, row);
            settingsPanel.Controls.Add(value, 1, row);
        }

        var row = 0;
        Row(_labelGame, gamePathRow, row++);
        Row(_labelSubs, _subtitleLang, row++);
        Row(_labelTts, _ttsLang, row++);
        var ttsRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Dock = DockStyle.Fill };
        ttsRow.Controls.Add(_ttsVol);
        ttsRow.Controls.Add(_ttsVolLabel);
        Row(_labelTtsVol, ttsRow, row++);
        var speedRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Dock = DockStyle.Fill };
        speedRow.Controls.Add(_briefSpeed);
        speedRow.Controls.Add(_briefSpeedLabel);
        Row(_labelBriefSpeed, speedRow, row++);
        var sfxRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Dock = DockStyle.Fill };
        sfxRow.Controls.Add(_sfxVol);
        sfxRow.Controls.Add(_sfxVolLabel);
        Row(_labelSfx, sfxRow, row++);
        var musicRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Dock = DockStyle.Fill };
        musicRow.Controls.Add(_musicVol);
        musicRow.Controls.Add(_musicVolLabel);
        Row(_labelMusic, musicRow, row++);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10, 0, 10, 6),
        };
        buttons.Controls.AddRange(new Control[] { _save, _start, _stop });

        var logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        logPanel.Controls.Add(_log);

        Controls.Add(logPanel);
        Controls.Add(_logCaption);
        Controls.Add(buttons);
        Controls.Add(settingsPanel);

        LoadSettings();
    }

    private void ApplyLocalizedCaptions()
    {
        _labelGame.Text = UiStrings.LabelGameFolder;
        _labelSubs.Text = UiStrings.LabelSubtitles;
        _labelTts.Text = UiStrings.LabelBriefVoice;
        _labelTtsVol.Text = UiStrings.LabelTtsVolume;
        _labelBriefSpeed.Text = UiStrings.LabelBriefSpeed;
        _labelSfx.Text = UiStrings.LabelSfx;
        _labelMusic.Text = UiStrings.LabelMusic;
        _logCaption.Text = UiStrings.LogCaption;
        _browse.Text = UiStrings.Browse;
        _save.Text = UiStrings.Save;
        _start.Text = UiStrings.Start;
        _stop.Text = UiStrings.Stop;

        _subtitleLang.Items.Clear();
        _ttsLang.Items.Clear();
        _ttsLang.Items.AddRange(new object[] { UiStrings.VoiceRu, UiStrings.VoiceEn });
    }

    private void LoadSettings()
    {
        _settings = UserConfigStore.Load();
        _gameRoot.Text = _settings.GameRoot;
        RefreshHudLanguages(keepSavedGameLanguage: true);
        _ttsVol.Value = (int)Math.Round(_settings.TtsVolume * 100);
        _briefSpeed.Value = Math.Clamp((int)Math.Round(_settings.BriefPlaybackSpeed * 100), _briefSpeed.Minimum, _briefSpeed.Maximum);
        _sfxVol.Value = _settings.GameSfxVolume;
        _musicVol.Value = _settings.GameMusicVolume;

        var langs = UserConfigStore.AvailableTtsLanguages();
        if (langs.Count == 0)
            langs.Add(_settings.TtsLanguage);
        var pick = langs.Contains(_settings.TtsLanguage) ? _settings.TtsLanguage : langs[0];
        _ttsLang.SelectedIndex = pick == "en" ? 1 : 0;
        _ttsLang.Enabled = langs.Count > 1;

        UpdateVolLabels();
        AppendLog(UiStrings.ReadyHint);
    }

    private void SaveFromUi(bool logMsg)
    {
        var rawPath = _gameRoot.Text.Trim();
        _settings.GameRoot = string.IsNullOrEmpty(rawPath)
            ? AppPaths.ResolveGameFolder("..")
            : AppPaths.ResolveGameFolder(rawPath);
        _gameRoot.Text = _settings.GameRoot;
        if (_hudLangCodes.Count > 0 && _subtitleLang.SelectedIndex >= 0
            && _subtitleLang.SelectedIndex < _hudLangCodes.Count)
            _settings.GameLanguage = _hudLangCodes[_subtitleLang.SelectedIndex];
        _settings.TtsLanguage = _ttsLang.SelectedIndex == 1 ? "en" : "ru";
        _settings.TtsVolume = _ttsVol.Value / 100f;
        _settings.BriefPlaybackSpeed = _briefSpeed.Value / 100f;
        _settings.GameSfxVolume = _sfxVol.Value;
        _settings.GameMusicVolume = _musicVol.Value;
        UserConfigStore.Save(_settings);
        _session?.SetTtsVolume(_settings.TtsVolume);
        _session?.SetBriefPlaybackSpeed(_settings.BriefPlaybackSpeed);
        if (logMsg)
            AppendLog(UiStrings.SettingsSaved);
    }

    private void OnBrowse(object? sender, EventArgs e)
    {
        var start = _gameRoot.Text.Trim();
        if (string.IsNullOrEmpty(start))
            start = _settings.GameRoot;
        using var dlg = new FolderBrowserDialog
        {
            Description = UiStrings.BrowseDescription,
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(start) ? start : "",
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _gameRoot.Text = dlg.SelectedPath;
            RefreshHudLanguages();
        }
    }

    private void RefreshHudLanguages(bool keepSavedGameLanguage = false)
    {
        var raw = _gameRoot.Text.Trim();
        var gameRoot = string.IsNullOrEmpty(raw)
            ? AppPaths.ResolveGameFolder(_settings.GameRoot)
            : AppPaths.ResolveGameFolder(raw);
        var codes = GameGxtAvailability.HudLanguages(gameRoot).ToList();
        var prefer = keepSavedGameLanguage
            ? _settings.GameLanguage
            : _hudLangCodes.Count > 0 && _subtitleLang.SelectedIndex >= 0 && _subtitleLang.SelectedIndex < _hudLangCodes.Count
                ? _hudLangCodes[_subtitleLang.SelectedIndex]
                : _settings.GameLanguage;

        _hudLangCodes.Clear();
        _subtitleLang.Items.Clear();
        foreach (var code in codes)
        {
            _hudLangCodes.Add(code);
            _subtitleLang.Items.Add(code == "en" ? UiStrings.SubtitleEn : UiStrings.SubtitleRu);
        }

        _subtitleLang.Enabled = codes.Count > 0;
        if (codes.Count == 0)
            return;

        var pick = codes.Contains(prefer) ? prefer : codes[0];
        _subtitleLang.SelectedIndex = codes.IndexOf(pick);
    }

    private void OnStart(object? sender, EventArgs e)
    {
        SaveFromUi(logMsg: false);
        if (_session != null)
        {
            _session.Dispose();
            _session = null;
        }

        _session = new InjectSession(
            _settings,
            line =>
            {
                if (IsDisposed)
                    return;
                BeginInvoke(() => AppendLog(line));
            },
            () =>
            {
                if (IsDisposed)
                    return;
                BeginInvoke(() =>
                {
                    _start.Enabled = true;
                    _stop.Enabled = false;
                });
            });
        _session.Start();
        _start.Enabled = false;
        _stop.Enabled = true;
        AppendLog(UiStrings.SessionStarted);
    }

    private void OnStop(object? sender, EventArgs e)
    {
        _session?.Stop();
        _session?.Dispose();
        _session = null;
        _start.Enabled = true;
        _stop.Enabled = false;
        AppendLog(UiStrings.Stopping);
    }

    private void UpdateVolLabels()
    {
        _ttsVolLabel.Text = $"{_ttsVol.Value}%";
        _briefSpeedLabel.Text = $"{_briefSpeed.Value}%";
        _sfxVolLabel.Text = $"{_sfxVol.Value}";
        _musicVolLabel.Text = $"{_musicVol.Value}";
        if (_session != null && _session.IsRunning)
        {
            _session.SetTtsVolume(_ttsVol.Value / 100f);
            _session.SetBriefPlaybackSpeed(_briefSpeed.Value / 100f);
        }
    }

    private void AppendLog(string line)
    {
        var stamp = DateTime.Now.ToString("HH:mm:ss");
        _log.AppendText($"[{stamp}] {line}{Environment.NewLine}");
    }
}
