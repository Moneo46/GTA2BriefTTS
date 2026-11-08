namespace GTA2BriefTTS.User;

public sealed class UserSettings
{
    public string GameRoot { get; set; } = "..";
    public string GameLanguage { get; set; } = "ru";
    public string TtsLanguage { get; set; } = "ru";
    public float TtsVolume { get; set; } = 0.7f;
    /// <summary>Brief MP3 playback speed (1.0 = normal).</summary>
    public float BriefPlaybackSpeed { get; set; } = 1f;
    public string ProcessName { get; set; } = "GTA2.exe";
    public string BriefTrigger { get; set; } = "set";
    public bool AutoScanInProcess { get; set; } = true;
    public int GameSfxVolume { get; set; } = 127;
    public int GameMusicVolume { get; set; } = 127;
}
