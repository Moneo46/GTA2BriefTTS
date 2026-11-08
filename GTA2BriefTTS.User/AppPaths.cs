using System.Text.Json;
using System.Text.Json.Nodes;

namespace GTA2BriefTTS.User;

public static class AppPaths
{
    public static string AppRoot { get; } = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static void EnsureCwd()
    {
        try
        {
            Directory.SetCurrentDirectory(AppRoot);
        }
        catch (IOException)
        {
            /* ignore */
        }
    }

    public static string GameRoot(JsonDocument cfg)
    {
        var env = Environment.GetEnvironmentVariable("GTA2_ROOT")?.Trim();
        if (!string.IsNullOrEmpty(env))
            return Path.GetFullPath(env);

        var root = cfg.RootElement;
        string? configured = null;
        if (root.TryGetProperty("game_root", out var gr) && gr.ValueKind == JsonValueKind.String)
        {
            var raw = gr.GetString()!;
            configured = Path.IsPathRooted(raw)
                ? Path.GetFullPath(raw)
                : Path.GetFullPath(Path.Combine(AppRoot, raw));
            if (File.Exists(Path.Combine(configured, "GTA2.exe")))
                return configured;
        }

        foreach (var candidate in new[] { AppRoot, Path.GetDirectoryName(AppRoot)!, Path.GetDirectoryName(Path.GetDirectoryName(AppRoot)!)! })
        {
            if (string.IsNullOrEmpty(candidate))
                continue;
            if (File.Exists(Path.Combine(candidate, "GTA2.exe")))
                return Path.GetFullPath(candidate);
        }

        if (configured != null)
            return configured;
        return Path.GetFullPath(Path.Combine(AppRoot, ".."));
    }

  /// <summary>Resolve configured game_root (or GTA2_ROOT) to a full directory path.</summary>
    public static string ResolveGameFolder(string? configuredGameRoot)
    {
        var env = Environment.GetEnvironmentVariable("GTA2_ROOT")?.Trim();
        if (!string.IsNullOrEmpty(env))
            return Path.GetFullPath(env);

        var raw = string.IsNullOrWhiteSpace(configuredGameRoot) ? ".." : configuredGameRoot.Trim();
        var node = new JsonObject { ["game_root"] = raw };
        using var doc = JsonDocument.Parse(node.ToJsonString());
        return GameRoot(doc);
    }
}
