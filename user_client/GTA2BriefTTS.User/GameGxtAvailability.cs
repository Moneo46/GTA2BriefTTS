using System.Text.Json;

namespace GTA2BriefTTS.User;

internal static class GameGxtAvailability
{
    public static IReadOnlyList<string> HudLanguages(string gameRoot)
    {
        var root = AppPaths.ResolveGameFolder(gameRoot);
        if (!Directory.Exists(root))
            return Array.Empty<string>();

        JsonElement cfg;
        try
        {
            cfg = UserConfigStore.LoadConfigElement();
        }
        catch
        {
            cfg = default;
        }

        var found = new List<string>(2);
        foreach (var lang in new[] { "ru", "en" })
        {
            var path = cfg.ValueKind == JsonValueKind.Object
                ? LangConfig.GxtPath(cfg, root, lang)
                : Path.Combine(root, LangConfig.NormalizeLang(lang) == "en" ? "data/e.gxt" : "data/r.gxt");
            if (File.Exists(path))
                found.Add(lang);
        }

        return found;
    }
}
