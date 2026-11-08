using System.Text.Json;

namespace GTA2BriefTTS.User;

public static class AddressConfig
{
    public static Dictionary<string, object?> LoadForAgent(JsonElement cfg, bool debug)
    {
        var addrPath = Path.Combine(AppPaths.AppRoot, "addresses.json");
        if (!File.Exists(addrPath))
            throw new FileNotFoundException("addresses.json missing", addrPath);

        var addr = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(addrPath))
                   ?? new Dictionary<string, JsonElement>();
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in addr)
            result[kv.Key] = JsonElementToObject(kv.Value);

        var sigPath = Path.Combine(AppPaths.AppRoot, "brief_signatures.json");
        if (File.Exists(sigPath))
        {
            var sigs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(sigPath));
            if (sigs != null)
                result["signatures"] = sigs.ToDictionary(k => k.Key, v => JsonElementToObject(v.Value));
        }

        var autoScan = true;
        if (cfg.TryGetProperty("auto_scan_in_process", out var a) && a.ValueKind is JsonValueKind.False or JsonValueKind.True)
            autoScan = a.GetBoolean();
        result["auto_scan_in_process"] = autoScan;

        result["debug"] = debug;
        if (cfg.TryGetProperty("brief_trigger", out var bt) && bt.ValueKind == JsonValueKind.String)
            result["brief_trigger"] = bt.GetString() ?? "set";
        else
            result["brief_trigger"] = "set";

        return result;
    }

    public static string BuildAgentSource(JsonElement cfg, bool debug)
    {
        var addr = LoadForAgent(cfg, debug);
        var addrJson = JsonSerializer.Serialize(addr);
        var template = File.ReadAllText(Path.Combine(AppPaths.AppRoot, "frida_agent.js"));
        return template.Replace("__ADDR_JSON__", addrJson.Replace("\\", "\\\\").Replace("'", "\\'"));
    }

    private static object? JsonElementToObject(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var i) ? i : el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
        JsonValueKind.Array => el.EnumerateArray().Select(JsonElementToObject).ToList(),
        _ => null,
    };
}
