using System.Text.Json;
using System.Text.Json.Nodes;

namespace GTA2BriefTTS.User;

public static class UserConfigStore
{
    private static readonly string ConfigPath = System.IO.Path.Combine(AppPaths.AppRoot, "config.json");

    private static readonly JsonSerializerOptions WriteOpts = new() { WriteIndented = true };

    public static UserSettings Load()
    {
        var s = new UserSettings();
        if (!File.Exists(ConfigPath))
            return s;

        using var doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
        var root = doc.RootElement;
        if (root.TryGetProperty("game_root", out var gr) && gr.ValueKind == JsonValueKind.String)
            s.GameRoot = gr.GetString() ?? s.GameRoot;
        s.GameRoot = AppPaths.ResolveGameFolder(s.GameRoot);
        if (root.TryGetProperty("game_language", out var gl))
            s.GameLanguage = LangConfig.NormalizeLang(gl.GetString());
        if (root.TryGetProperty("tts_language", out var tl))
            s.TtsLanguage = LangConfig.NormalizeLang(tl.GetString());
        if (root.TryGetProperty("tts_volume", out var vol) && vol.TryGetSingle(out var fv))
            s.TtsVolume = fv;
        if (root.TryGetProperty("brief_playback_speed", out var bps) && bps.TryGetSingle(out var sp))
            s.BriefPlaybackSpeed = Math.Clamp(sp, 0.25f, 3f);
        if (root.TryGetProperty("process_name", out var pn) && pn.ValueKind == JsonValueKind.String)
            s.ProcessName = pn.GetString() ?? s.ProcessName;
        if (root.TryGetProperty("brief_trigger", out var bt) && bt.ValueKind == JsonValueKind.String)
            s.BriefTrigger = bt.GetString() ?? s.BriefTrigger;
        if (root.TryGetProperty("auto_scan_in_process", out var scan))
            s.AutoScanInProcess = scan.ValueKind != JsonValueKind.False;

        var sound = GameSoundSettings.Read();
        s.GameSfxVolume = sound.Sfx;
        s.GameMusicVolume = sound.Music;
        return s;
    }

    public static void Save(UserSettings s)
    {
        JsonObject root;
        if (File.Exists(ConfigPath))
            root = JsonNode.Parse(File.ReadAllText(ConfigPath))!.AsObject();
        else
            root = new JsonObject();

        root["game_root"] = AppPaths.ResolveGameFolder(s.GameRoot);
        root["game_language"] = s.GameLanguage;
        root["tts_language"] = s.TtsLanguage;
        root["tts_volume"] = Math.Round(s.TtsVolume, 3);
        root["brief_playback_speed"] = Math.Round(s.BriefPlaybackSpeed, 2);
        root["process_name"] = s.ProcessName;
        root["brief_trigger"] = s.BriefTrigger;
        root["auto_scan_in_process"] = s.AutoScanInProcess;
        root["record_session"] = false;
        root["tts_bake_on_start"] = false;
        root["auto_addresses"] = false;

        foreach (var hidden in new[]
                 {
                     "gang_voices", "gang_voices_en", "tts_base_voice", "tts_default_voice",
                     "brief_ids", "brief_ids_tutorial_full",
                 })
            root.Remove(hidden);

        File.WriteAllText(ConfigPath, root.ToJsonString(WriteOpts) + Environment.NewLine);
        GameSoundSettings.Write(s.GameSfxVolume, s.GameMusicVolume);
    }

    public static JsonElement LoadConfigElement()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
        return doc.RootElement.Clone();
    }

    public static List<string> AvailableTtsLanguages()
    {
        var cacheRoot = System.IO.Path.Combine(AppPaths.AppRoot, "tts_cache");
        var list = new List<string>();
        foreach (var code in new[] { "ru", "en" })
        {
            var dir = System.IO.Path.Combine(cacheRoot, code);
            if (Directory.Exists(dir) && Directory.EnumerateFiles(dir, "*.mp3", SearchOption.AllDirectories).Any())
                list.Add(code);
        }

        return list;
    }
}
