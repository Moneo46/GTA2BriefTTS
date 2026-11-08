using System.Text.Json;
using Frida;

namespace GTA2BriefTTS.User;

internal static class FridaMessageHandler
{
    public static void OnMessage(
        ScriptMessageEventArgs ev,
        BriefSpeaker speaker,
        Dictionary<string, BriefLine> briefs,
        InjectState state,
        Action<string> log,
        bool showDebug)
    {
        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(ev.Message);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var t) && t.GetString() == "error")
            {
                log(UiStrings.AgentFail);
                return;
            }

            if (root.TryGetProperty("type", out var mt) && mt.GetString() != "send")
                return;

            if (!root.TryGetProperty("payload", out var payload))
                return;

            if (!payload.TryGetProperty("type", out var kind))
                return;

            switch (kind.GetString())
            {
                case "ready":
                    log(UiStrings.AgentReady);
                    speaker.PlayReady("n");
                    break;
                case "error":
                    log(UiStrings.AgentError(JsonUtil.GetPropertyOrDefault(payload, "msg")));
                    break;
                case "debug" when showDebug:
                    log(JsonUtil.GetPropertyOrDefault(payload, "msg"));
                    break;
                case "brief":
                    var bid = (JsonUtil.GetPropertyOrDefault(payload, "id") ?? "").Trim();
                    if (string.IsNullOrEmpty(bid) || bid == state.LastBrief)
                        return;
                    if (!briefs.TryGetValue(bid, out var line))
                    {
                        log(UiStrings.BriefNoText(bid));
                        return;
                    }

                    state.LastBrief = bid;
                    var preview = line.Text.Length > 72 ? line.Text[..72] + "…" : line.Text;
                    log(UiStrings.BriefLine(bid, preview));
                    speaker.PlayBrief(bid, line.Portrait, line.Text);
                    break;
            }
        }
        catch (Exception ex)
        {
            log(UiStrings.MessageParse(ex.Message));
        }
        finally
        {
            doc?.Dispose();
        }
    }
}

internal static class JsonUtil
{
    public static string GetPropertyOrDefault(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) ? v.ToString() : "";
}
