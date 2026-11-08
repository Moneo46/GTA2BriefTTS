using Microsoft.Win32;

namespace GTA2BriefTTS.User;

public static class GameLanguage
{
    private static readonly (RegistryKey Hive, string SubKey)[] Paths =
    {
        (Registry.CurrentUser, @"Software\DMA Design Ltd\GTA2\Option"),
        (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\DMA Design Ltd\GTA2\Option"),
        (Registry.CurrentUser, @"Software\Classes\VirtualStore\MACHINE\SOFTWARE\WOW6432Node\DMA Design Ltd\GTA2\Option"),
    };

    public static bool Apply(string lang)
    {
        var code = LangConfig.NormalizeLang(lang) == "ru" ? "r" : "e";
        var ok = false;
        foreach (var (hive, sub) in Paths)
        {
            try
            {
                using var key = hive.CreateSubKey(sub, true);
                if (key == null)
                    continue;
                key.SetValue("language", code, RegistryValueKind.String);
                key.SetValue("Language", code, RegistryValueKind.String);
                ok = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[inject] game_language: {sub}: {ex.Message}");
            }
        }

        if (ok)
            Console.WriteLine($"[inject] Game subtitles language set to {LangConfig.NormalizeLang(lang)} ({code}.gxt)");
        else
            Console.Error.WriteLine("[inject] game_language: registry update failed");
        return ok;
    }

    public static bool IsProcessRunning(string name)
    {
        var target = name.ToLowerInvariant();
        foreach (var proc in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                if (proc.ProcessName.Equals(Path.GetFileNameWithoutExtension(target), StringComparison.OrdinalIgnoreCase)
                    || proc.MainModule?.ModuleName.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
            }
            catch
            {
                /* access denied for some processes */
            }
        }

        return false;
    }
}
