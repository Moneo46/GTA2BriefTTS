using System.Text;
using System.Text.Json;

namespace GTA2BriefTTS.User;

/// <summary>Embedded voice/brief tables (pack_data.dat), not exposed in config.json.</summary>
internal static class PackDataStore
{
    private static readonly byte[] Magic = "G2BT\x01"u8.ToArray();
    private static readonly byte[] Key = Encoding.ASCII
        .GetBytes("GTA2BriefTTS-pack-v1-2025")
        .Select(b => (byte)(b ^ 0xA7))
        .ToArray();

    private static JsonElement? _root;

    private static JsonElement Root => _root ??= Load();

    private static JsonElement Load()
    {
        var path = Path.Combine(AppPaths.AppRoot, "pack_data.dat");
        if (!File.Exists(path))
            return default;

        var blob = File.ReadAllBytes(path);
        if (blob.Length < Magic.Length + 4 || !blob.AsSpan(0, Magic.Length).SequenceEqual(Magic))
            return default;

        var len = BitConverter.ToInt32(blob, Magic.Length);
        if (len <= 0 || Magic.Length + 4 + len > blob.Length)
            return default;

        var enc = blob.AsSpan(Magic.Length + 4, len).ToArray();
        for (var i = 0; i < enc.Length; i++)
            enc[i] ^= Key[i % Key.Length];

        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(enc));
        return doc.RootElement.Clone();
    }

    public static IReadOnlyDictionary<string, string> GangOverrides(string lang)
    {
        var code = LangConfig.NormalizeLang(lang);
        var key = code == "en" ? "gang_voices_en" : "gang_voices";
        if (Root.ValueKind != JsonValueKind.Object || !Root.TryGetProperty(key, out var obj)
            || obj.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, string>();

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in obj.EnumerateObject())
        {
            var v = prop.Value.GetString();
            if (!string.IsNullOrWhiteSpace(v))
                map[prop.Name.ToLowerInvariant()] = v!;
        }

        return map;
    }

    public static string? DefaultVoiceOverride()
    {
        if (Root.ValueKind != JsonValueKind.Object)
            return null;
        foreach (var name in new[] { "tts_default_voice", "tts_base_voice" })
        {
            if (Root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }
        }

        return null;
    }
}
