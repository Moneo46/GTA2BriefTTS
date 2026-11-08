namespace GTA2BriefTTS.User;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        AppPaths.EnsureCwd();
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
