namespace GTA2BriefTTS.User;

/// <summary>Default gang→Edge voice mapping (not shown in user config.json).</summary>
public static class VoiceCatalog
{
    private static readonly Dictionary<string, string> DefaultRu = new()
    {
        ["n"] = "en-US-AndrewMultilingualNeural",
        ["z"] = "en-US-BrianMultilingualNeural",
        ["m"] = "ru-RU-DmitryNeural",
        ["y"] = "ko-KR-HyunsuMultilingualNeural",
        ["k"] = "fr-FR-RemyMultilingualNeural",
        ["l"] = "it-IT-GiuseppeMultilingualNeural",
        ["r"] = "en-AU-WilliamMultilingualNeural",
        ["s"] = "de-DE-FlorianMultilingualNeural",
    };

    private static readonly Dictionary<string, string> DefaultEn = new()
    {
        ["n"] = "en-US-AndrewNeural",
        ["z"] = "en-US-BrianNeural",
        ["m"] = "en-US-GuyNeural",
        ["y"] = "en-US-ChristopherNeural",
        ["k"] = "en-IN-PrabhatNeural",
        ["l"] = "en-US-EricNeural",
        ["r"] = "en-AU-WilliamNeural",
        ["s"] = "en-GB-ThomasNeural",
    };

    public static IReadOnlyDictionary<string, string> GangVoices(string lang) =>
        LangConfig.NormalizeLang(lang) == "en"
            ? DefaultEn
            : DefaultRu;

    public static string DefaultVoice(string lang) =>
        LangConfig.NormalizeLang(lang) == "en" ? "en-US-AndrewNeural" : "ru-RU-DmitryNeural";
}
