using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Thinker.Services;

public sealed class PowerCfgRunner : IPowerCfgRunner
{
    static PowerCfgRunner()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<string> RunAsync(string arguments, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "powercfg.exe",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = GetPowerCfgOutputEncoding(),
            StandardErrorEncoding = GetPowerCfgOutputEncoding()
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"powercfg {arguments} failed with exit code {process.ExitCode}: {stderr.Trim()}");
        }

        return stdout;
    }

    private static Encoding GetPowerCfgOutputEncoding()
    {
        return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
    }
}
