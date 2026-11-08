using System.Globalization;

namespace GTA2BriefTTS.User;

internal static class UiStrings
{
    private static bool Ru =>
        string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "ru", StringComparison.OrdinalIgnoreCase);

    public static string WindowTitle => "GTA2 Brief TTS";

    public static string LabelGameFolder => Ru ? "Папка GTA2:" : "GTA2 folder:";
    public static string LabelSubtitles => Ru ? "Субтитры в игре:" : "In-game subtitles:";
    public static string LabelBriefVoice => Ru ? "Озвучка брифов:" : "Brief voice:";
    public static string LabelTtsVolume => Ru ? "Громкость озвучки:" : "Brief volume:";
    public static string LabelBriefSpeed => Ru ? "Скорость брифов:" : "Brief speed:";
    public static string LabelSfx => Ru ? "Эффекты в игре:" : "Game SFX:";
    public static string LabelMusic => Ru ? "Музыка в игре:" : "Game music:";
    public static string LogCaption => Ru ? "Журнал" : "Log";

    public static string Browse => Ru ? "Обзор…" : "Browse…";
    public static string Save => Ru ? "Сохранить" : "Save";
    public static string Start => Ru ? "Запуск" : "Start";
    public static string Stop => Ru ? "Стоп" : "Stop";

    public static string SubtitleRu => Ru ? "Русский (субтитры)" : "Russian (HUD)";
    public static string SubtitleEn => Ru ? "English (субтитры)" : "English (HUD)";
    public static string VoiceRu => Ru ? "Русский" : "Russian";
    public static string VoiceEn => "English";

    public static string BrowseDescription => Ru ? "Папка, где лежит GTA2.exe" : "Folder containing GTA2.exe";

    public static string ReadyHint => Ru
        ? "Укажите папку игры и нажмите «Запуск» (игра запустится, если ещё не открыта)."
        : "Set the game folder and click Start (the game will launch if it is not running yet).";

    public static string SettingsSaved => Ru ? "Настройки сохранены." : "Settings saved.";
    public static string SessionStarted => Ru ? "Сеанс запущен." : "Session started.";
    public static string Stopping => Ru ? "Остановка…" : "Stopping…";

    public static string GameAlreadyRunning => Ru
        ? "GTA2 уже запущена — субтитры обновятся после перезапуска игры."
        : "GTA2 is already running — subtitle language applies after a restart.";

    public static string GxtMissing(string path) => Ru
        ? $"Не найден файл субтитров: {path}"
        : $"Subtitle file not found: {path}";

    public static string SetGameFolderHint => Ru
        ? "Укажите папку с GTA2.exe или переменную GTA2_ROOT."
        : "Set the folder with GTA2.exe or the GTA2_ROOT environment variable.";

    public static string BriefsLoaded(int count, string lang) => Ru
        ? $"Загружено {count} брифов (озвучка: {lang})."
        : $"Loaded {count} briefs (voice: {lang}).";

    public static string WaitingForGame(string proc) => Ru
        ? $"Ожидание {proc}…"
        : $"Waiting for {proc}…";

    public static string Cancelled => Ru ? "Остановлено." : "Stopped.";
    public static string GameFound => Ru
        ? "Игра найдена. Брифы озвучиваются при показе на экране."
        : "Game found. Briefs play when shown on screen.";

    public static string GameExited => Ru ? "Игра завершена." : "Game exited.";
    public static string SessionStopped => Ru ? "Сеанс остановлен." : "Session stopped.";
    public static string Error(string msg) => Ru ? $"Ошибка: {msg}" : $"Error: {msg}";

    public static string LaunchingGame => Ru ? "Запуск GTA2…" : "Launching GTA2…";
    public static string LaunchFailed(string msg) => Ru ? $"Не удалось запустить игру: {msg}" : $"Could not start the game: {msg}";

    public static string AgentReady => Ru ? "Связь с игрой установлена." : "Connected to the game.";
    public static string AgentFail => Ru ? "Сбой модуля в игре. Перезапустите GTA2." : "In-game module failed. Restart GTA2.";
    public static string BriefNoText(string bid) => Ru
        ? $"Бриф {bid}: нет текста для выбранного языка."
        : $"Brief {bid}: no text for the selected language.";

    public static string BriefLine(string bid, string preview) => Ru
        ? $"Бриф {bid}: {preview}"
        : $"Brief {bid}: {preview}";

    public static string AgentError(string msg) => Ru ? $"Ошибка: {msg}" : $"Error: {msg}";
    public static string MessageParse(string msg) => Ru ? $"Сообщение от игры: {msg}" : $"Message from game: {msg}";
}
