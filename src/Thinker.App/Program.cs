using Thinker.Services;

namespace Thinker;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var runner = new PowerCfgRunner();
        var powerSettings = new PowerSettingsService(runner);
        var stateStore = new StateStore();
        var clock = new SystemClock();
        var startupService = new StartupService(Environment.ProcessPath ?? Application.ExecutablePath);
        using var context = new TrayApplicationContext(powerSettings, stateStore, clock, startupService);

        Application.Run(context);
    }
}
