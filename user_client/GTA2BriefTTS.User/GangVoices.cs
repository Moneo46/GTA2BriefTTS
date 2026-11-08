using System.Text.RegularExpressions;

namespace GTA2BriefTTS.User;

public static class GangVoices
{
    private static readonly Dictionary<string, (string Rate, string Pitch)> Prosody = new()
    {
        ["n"] = ("+0%", "+0Hz"),
        ["z"] = ("-8%", "-8Hz"),
        ["m"] = ("-6%", "-4Hz"),
        ["y"] = ("+6%", "+2Hz"),
        ["k"] = ("-12%", "+8Hz"),
        ["l"] = ("+18%", "+12Hz"),
        ["r"] = ("-14%", "-10Hz"),
        ["s"] = ("-2%", "+4Hz"),
    };

    public static Dictionary<string, string> ForLang(string lang)
    {
        var map = new Dictionary<string, string>(VoiceCatalog.GangVoices(lang), StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in PackDataStore.GangOverrides(lang))
            map[k] = v;
        return map;
    }

    public static string DefaultVoice(string lang)
    {
        var o = PackDataStore.DefaultVoiceOverride();
        if (!string.IsNullOrWhiteSpace(o))
            return o!;
        return VoiceCatalog.DefaultVoice(lang);
    }

    public static string VoiceForGang(string? gang, IReadOnlyDictionary<string, string> map, string defaultVoice)
    {
        if (!string.IsNullOrEmpty(gang) && map.TryGetValue(gang.ToLowerInvariant(), out var v))
            return v;
        return defaultVoice;
    }

    public static string CacheVoiceId(string voice, string? gang)
    {
        var key = (gang ?? "n").ToLowerInvariant();
        var (rate, pitch) = Prosody.TryGetValue(key, out var p) ? p : ("+0%", "+0Hz");
        var tag = $"{rate}_{pitch}".Replace("%", "pct").Replace("+", "p").Replace("-", "m");
        return $"{voice}__{tag}";
    }

    public static string VoiceSlug(string voice) =>
        Regex.Replace(voice, @"[^\w.\-]+", "_");
}
