using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SatisfactoryServerGUI.Models;

namespace SatisfactoryServerGUI.Services;

public class SteamCmdService : ISteamCmdService
{
    public event EventHandler<ConsoleEntry>? OutputReceived;

    public bool IsBusy { get; private set; }

    public async Task<bool> InstallOrUpdateAsync(
        string steamCmdPath,
        string installDir,
        string betaBranch = "public",
        CancellationToken ct = default)
    {
        if (IsBusy)
        {
            Emit("SteamCMD is already running.", ConsoleEntrySeverity.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(steamCmdPath) || !File.Exists(steamCmdPath))
        {
            Emit($"SteamCMD not found: {steamCmdPath}", ConsoleEntrySeverity.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(installDir))
        {
            Emit("Install directory is not set.", ConsoleEntrySeverity.Error);
            return false;
        }

        Directory.CreateDirectory(installDir);

        var args = new StringBuilder();
        args.Append("+force_install_dir \"").Append(installDir).Append("\" ");
        args.Append("+login anonymous ");
        args.Append("+app_update ").Append(LocalServerSettings.AppId);
        if (!string.IsNullOrWhiteSpace(betaBranch) &&
            !betaBranch.Equals("public", StringComparison.OrdinalIgnoreCase))
        {
            args.Append(" -beta ").Append(betaBranch);
        }
        args.Append(" validate +quit");

        var psi = new ProcessStartInfo
        {
            FileName = steamCmdPath,
            Arguments = args.ToString(),
            WorkingDirectory = Path.GetDirectoryName(steamCmdPath) ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        IsBusy = true;
        Emit($"Running SteamCMD app_update {LocalServerSettings.AppId} ({betaBranch})...", ConsoleEntrySeverity.Command);

        try
        {
            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => { if (e.Data != null) Emit(e.Data, ConsoleEntrySeverity.Info); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) Emit(e.Data, ConsoleEntrySeverity.Error); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            var ok = process.ExitCode == 0;
            Emit(ok ? "SteamCMD finished successfully." : $"SteamCMD exited with code {process.ExitCode}.",
                ok ? ConsoleEntrySeverity.Response : ConsoleEntrySeverity.Error);
            return ok;
        }
        catch (OperationCanceledException)
        {
            Emit("SteamCMD cancelled.", ConsoleEntrySeverity.Warning);
            return false;
        }
        catch (Exception ex)
        {
            Emit($"SteamCMD failed: {ex.Message}", ConsoleEntrySeverity.Error);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Emit(string text, ConsoleEntrySeverity severity)
        => OutputReceived?.Invoke(this, new ConsoleEntry(text, severity));
}
