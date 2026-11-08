namespace GTA2BriefTTS.User;

public sealed class BriefSpeaker
{
    private readonly IReadOnlyDictionary<string, string> _gangVoices;
    private readonly string _defaultVoice;
    private readonly TtsCache _cache;
    private readonly Mp3QueuePlayer _player;

    public BriefSpeaker(string lang, Mp3QueuePlayer player)
    {
        _gangVoices = GangVoices.ForLang(lang);
        _defaultVoice = GangVoices.DefaultVoice(lang);
        _cache = new TtsCache(lang);
        _player = player;
    }

    public string VoiceFor(string? gang)
    {
        var voice = GangVoices.VoiceForGang(gang, _gangVoices, _defaultVoice);
        return GangVoices.CacheVoiceId(voice, gang);
    }

    public void PlayBrief(string briefId, string? gang, string _)
    {
        var stored = VoiceFor(gang);
        var path = _cache.ResolveBriefMp3(stored, briefId);
        _player.Enqueue(path);
    }

    public void PlayReady(string? gang = "n")
    {
        var stored = VoiceFor(gang);
        _player.PlayReadyOrSkip(_cache, stored);
    }
}
