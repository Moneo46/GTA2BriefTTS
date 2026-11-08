using System.Text.RegularExpressions;

namespace GTA2BriefTTS.User;

public sealed class TtsCache
{
    private readonly string _lang;
    private readonly string _root;

    public TtsCache(string lang)
    {
        _lang = LangConfig.NormalizeLang(lang);
        _root = Path.Combine(AppPaths.AppRoot, "tts_cache", _lang);
    }

    public string? PathFor(string voiceFolder, string briefId, string ext = "mp3")
    {
        var safeKey = Regex.Replace(briefId, @"[^\w.\-]+", "_");
        if (safeKey.Length > 120)
            safeKey = safeKey[..120];
        var path = Path.Combine(_root, GangVoices.VoiceSlug(voiceFolder), $"{safeKey}.{ext}");
        return File.Exists(path) ? path : null;
    }

    public string ResolveBriefMp3(string voiceFolder, string briefId)
    {
        var hit = PathFor(voiceFolder, briefId);
        if (hit == null)
            throw new FileNotFoundException($"no cached audio for {briefId} ({voiceFolder})");
        return hit;
    }
}
