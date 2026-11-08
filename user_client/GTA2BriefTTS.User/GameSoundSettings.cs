using Microsoft.Win32;

namespace GTA2BriefTTS.User;

/// <summary>GTA2.exe reads DMA Design Ltd\GTA2\Sound (CDVol = music, SFXVol = effects).</summary>
public static class GameSoundSettings
{
    public const int MaxVolume = 127;

    private static readonly (RegistryKey Hive, string SubKey)[] Paths =
    {
        (Registry.CurrentUser, @"Software\DMA Design Ltd\GTA2\Sound"),
        (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\DMA Design Ltd\GTA2\Sound"),
        (
            Registry.CurrentUser,
            @"Software\Classes\VirtualStore\MACHINE\SOFTWARE\WOW6432Node\DMA Design Ltd\GTA2\Sound"
        ),
    };

    public static (int Sfx, int Music) Read()
    {
        foreach (var (hive, sub) in Paths)
        {
            try
            {
                using var key = hive.OpenSubKey(sub, false);
                if (key == null)
                    continue;
                var sfx = ReadDword(key, "SFXVol", 64);
                var music = ReadDword(key, "CDVol", 64);
                return (Clamp(sfx), Clamp(music));
            }
            catch
            {
                /* next hive */
            }
        }

        return (64, 64);
    }

    public static void Write(int sfx, int music)
    {
        sfx = Clamp(sfx);
        music = Clamp(music);
        foreach (var (hive, sub) in Paths)
        {
            try
            {
                using var key = hive.CreateSubKey(sub, true);
                if (key == null)
                    continue;
                key.SetValue("SFXVol", sfx, RegistryValueKind.DWord);
                key.SetValue("CDVol", music, RegistryValueKind.DWord);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private static int ReadDword(RegistryKey key, string name, int fallback)
    {
        var v = key.GetValue(name);
        return v switch
        {
            int i => i,
            byte b => b,
            _ => fallback,
        };
    }

    private static int Clamp(int v) => Math.Clamp(v, 0, MaxVolume);
}
