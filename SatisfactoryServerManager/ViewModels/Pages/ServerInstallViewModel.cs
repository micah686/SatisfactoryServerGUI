using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerManager.Helpers;
using SteamCmdWrapperLib;

namespace SatisfactoryServerManager.ViewModels.Pages;

public partial class ServerInstallViewModel: ViewModelBase
{
    [RelayCommand]
    private async Task InstallSteamCmdCommand()
    {
        try
        {
            var dataPath = Globals.DataPath
                ?? throw new InvalidOperationException("The application data path is not configured.");
            var installDirectory = Path.Combine(dataPath, "steamcmd");
            var steamCmdPath = GetSteamCmdPath(installDirectory);

            if (!File.Exists(steamCmdPath))
            {
                LogSteamCmdOutput($"SteamCMD was not found at '{steamCmdPath}'. Installing it now.");
                steamCmdPath = await SteamCmdHelper.InstallAsync(installDirectory);
            }
            else
            {
                LogSteamCmdOutput($"Using the existing SteamCMD installation at '{steamCmdPath}'.");
            }

            await using var wrapper = new SteamCmdWrapper(steamCmdPath);
            wrapper.OutputReceived += (_, args) => LogSteamCmdOutput(args.Text);

            LogSteamCmdOutput("Logging in to Steam anonymously.");
            var result = await wrapper.ExecuteAsync(["+login", "anonymous", "+quit"]);
            LogSteamCmdOutput($"SteamCMD exited with code {result.ExitCode?.ToString() ?? "unknown"}.");
        }
        catch (Exception exception)
        {
            LogSteamCmdOutput($"SteamCMD failed: {exception}");
        }
    }

    private static string GetSteamCmdPath(string installDirectory)
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(installDirectory, "steamcmd.exe");
        }

        if (OperatingSystem.IsLinux())
        {
            return Path.Combine(installDirectory, "steamcmd.sh");
        }

        throw new PlatformNotSupportedException("SteamCMD is only supported on Windows and Linux.");
    }

    private static void LogSteamCmdOutput(string message)
    {
        var logMessage = $"[SteamCMD] {message}";
        Console.WriteLine(logMessage);
        Trace.WriteLine(logMessage);
    }
}
