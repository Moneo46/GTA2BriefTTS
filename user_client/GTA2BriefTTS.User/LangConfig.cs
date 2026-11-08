using System.Text.Json;

namespace GTA2BriefTTS.User;

public static class LangConfig
{
    private static readonly Dictionary<string, string> DefaultGxt = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ru"] = "data/r.gxt",
        ["en"] = "data/e.gxt",
    };

    private static readonly Dictionary<string, string> ReadyPhrase = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ru"] = "Ок.",
        ["en"] = "OK.",
    };

    public static string NormalizeLang(string? value, string fallback = "ru")
    {
        var lang = (value ?? fallback).Trim().ToLowerInvariant();
        return lang is "ru" or "en" ? lang : fallback;
    }

    public static string TtsLanguage(JsonElement cfg)
    {
        if (cfg.TryGetProperty("tts_language", out var tts))
            return NormalizeLang(tts.GetString());
        if (cfg.TryGetProperty("game_language", out var gl))
            return NormalizeLang(gl.GetString());
        return "ru";
    }

    public static string GameLanguage(JsonElement cfg) =>
        cfg.TryGetProperty("game_language", out var gl)
            ? NormalizeLang(gl.GetString())
            : "ru";

    public static string ReadyPhraseFor(string lang) =>
        ReadyPhrase.TryGetValue(NormalizeLang(lang), out var p) ? p : ReadyPhrase["ru"];

    public static string GxtRelative(JsonElement cfg, string lang)
    {
        if (cfg.TryGetProperty("gxt_files", out var files) && files.TryGetProperty(lang, out var path))
        {
            var s = path.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s!;
        }

        if (lang == "ru" && cfg.TryGetProperty("gxt_relative", out var legacy))
        {
            var s = legacy.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s!;
        }

        return DefaultGxt.TryGetValue(lang, out var d) ? d : DefaultGxt["ru"];
    }

    public static string GxtPath(JsonElement cfg, string gameRoot, string lang) =>
        Path.Combine(gameRoot, GxtRelative(cfg, lang));
}
