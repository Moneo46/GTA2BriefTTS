using System.Text;
using System.Text.RegularExpressions;

namespace GTA2BriefTTS.User;

public sealed record BriefLine(string Text, string? Portrait);

public static class GxtReader
{
    private const string BukaOrder =
        "ЁЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮёйцукенгшщзхъфывапролджэячсмитьбю";

    private static readonly Dictionary<byte, char> Buka = BukaOrder
        .Select((ch, i) => ((byte)(0xB1 + i), ch))
        .ToDictionary(t => t.Item1, t => t.Item2);

    public static Dictionary<string, BriefLine> LoadBriefLines(string gxtPath)
    {
        var entries = LoadGxtChunks(gxtPath);
        var ids = entries.Keys.Where(k => k.All(char.IsDigit)).OrderBy(int.Parse).ToList();
        var result = new Dictionary<string, BriefLine>();
        foreach (var bid in ids)
        {
            if (!entries.TryGetValue(bid, out var raw) || raw.Length == 0)
                continue;
            var portrait = PortraitFromRaw(raw);
            var text = DecodeGxtText(raw);
            if (string.IsNullOrWhiteSpace(text))
                continue;
            result[bid] = new BriefLine(text, portrait);
        }

        return result;
    }

    private static Dictionary<string, byte[]> LoadGxtChunks(string path)
    {
        var data = File.ReadAllBytes(path);
        var pos = 6;
        var chunks = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        while (pos + 8 <= data.Length)
        {
            var name = Encoding.ASCII.GetString(data, pos, 4);
            var ln = BitConverter.ToUInt32(data, pos + 4);
            chunks[name] = data.AsSpan(pos + 8, (int)ln).ToArray();
            pos += 8 + (int)ln;
        }

        if (!chunks.TryGetValue("TKEY", out var tkey) || !chunks.TryGetValue("TDAT", out var tdat))
            return new Dictionary<string, byte[]>();

        var raw = new List<(int Off, string Key)>();
        for (var i = 0; i + 12 <= tkey.Length; i += 12)
        {
            var off = BitConverter.ToInt32(tkey, i);
            var key = Encoding.ASCII.GetString(tkey, i + 4, 8).TrimEnd('\0');
            raw.Add((off, key));
        }

        var order = raw.Select((_, n) => n).OrderBy(n => (raw[n].Off, n)).ToList();
        var entries = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        for (var rank = 0; rank < order.Count; rank++)
        {
            var n = order[rank];
            var (off, key) = raw[n];
            var end = rank + 1 < order.Count ? raw[order[rank + 1]].Off : tdat.Length;
            if (end < off || end > tdat.Length)
                end = tdat.Length;
            var slice = new byte[end - off];
            Array.Copy(tdat, off, slice, 0, slice.Length);
            entries[key] = slice;
        }

        return entries;
    }

    private static string? PortraitFromRaw(byte[] raw)
    {
        foreach (var part in SplitUtf16Strings(raw))
        {
            for (var i = 0; i + 1 < part.Length; i += 2)
            {
                var lo = part[i];
                var hi = part[i + 1];
                if (hi == 0x21 && char.IsLetter((char)lo))
                    return char.ToLowerInvariant((char)lo).ToString();
                return null;
            }
        }

        return null;
    }

    private static List<byte[]> SplitUtf16Strings(byte[] raw)
    {
        var parts = new List<byte[]>();
        var i = 0;
        while (i < raw.Length)
        {
            var start = i;
            while (i + 1 < raw.Length)
            {
                if (raw[i] == 0 && raw[i + 1] == 0)
                {
                    var chunk = raw.AsSpan(start, i - start).ToArray();
                    if (chunk.Length > 0)
                        parts.Add(chunk);
                    i += 2;
                    break;
                }

                i += 2;
            }

            if (i + 1 >= raw.Length)
            {
                var tail = raw.AsSpan(start).ToArray();
                if (tail.Length > 0)
                    parts.Add(tail);
                break;
            }
        }

        return parts;
    }

    private static string DecodeGxtText(byte[] raw)
    {
        var parts = SplitUtf16Strings(raw);
        if (parts.Count == 0)
            return "";
        return DecodeUtf16(parts[0]);
    }

    private static string DecodeUtf16(byte[] raw)
    {
        var sb = new StringBuilder();
        var skippedPortrait = false;
        for (var i = 0; i + 1 < raw.Length; i += 2)
        {
            var lo = raw[i];
            var hi = raw[i + 1];
            if (hi == 0)
            {
                if (lo == 0)
                    continue;
                if (Buka.TryGetValue(lo, out var c))
                    sb.Append(c);
                else if (lo < 128)
                    sb.Append((char)lo);
            }
            else
            {
                var code = lo | (hi << 8);
                if (!skippedPortrait && hi == 0x21 && char.IsLetter((char)lo))
                {
                    skippedPortrait = true;
                    continue;
                }

                sb.Append((char)code);
            }
        }

        var s = sb.ToString().Replace("\0", "");
        s = Regex.Replace(s, @"[\u2160-\u217F\u2460-\u2473]", "");
        s = Regex.Replace(s, @"^[a-z]!", "", RegexOptions.IgnoreCase);
        s = s.Replace("#", "");
        return string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
