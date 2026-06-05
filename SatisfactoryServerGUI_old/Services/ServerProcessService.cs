using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SatisfactoryServerGUI.Models;

namespace SatisfactoryServerGUI.Services;

public class ServerProcessService : IServerProcessService
{
    private Process? _process;

    public event EventHandler<ConsoleEntry>? OutputReceived;
    public event EventHandler<bool>? RunningChanged;

    public bool IsRunning => _process is { HasExited: false };
    public DateTime? StartTime { get; private set; }

    public Task<bool> StartAsync(string exePath, string arguments, string? workingDir = null)
    {
        if (IsRunning)
        {
            Emit("Server is already running.", ConsoleEntrySeverity.Warning);
            return Task.FromResult(false);
        }

        if (!File.Exists(exePath))
        {
            Emit($"Server executable not found: {exePath}", ConsoleEntrySeverity.Error);
            return Task.FromResult(false);
        }

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var fileName = exePath;
        var args = arguments;

        // On Linux the launcher is FactoryServer.sh — run it through bash.
        if (!isWindows && exePath.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))
        {
            fileName = "/bin/bash";
            args = $"\"{exePath}\" {arguments}";
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = workingDir ?? Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) Emit(e.Data, ConsoleEntrySeverity.Info);
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) Emit(e.Data, ConsoleEntrySeverity.Error);
            };
            _process.Exited += OnExited;

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            StartTime = DateTime.Now;
            Emit($"Started: {fileName} {args}", ConsoleEntrySeverity.Command);
            RunningChanged?.Invoke(this, true);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Emit($"Failed to start server: {ex.Message}", ConsoleEntrySeverity.Error);
            _process = null;
            return Task.FromResult(false);
        }
    }

    public async Task StopAsync()
    {
        if (_process is null || _process.HasExited)
            return;

        Emit("Stopping server...", ConsoleEntrySeverity.Command);

        try
        {
            _process.CloseMainWindow();
        }
        catch
        {
            // No main window (headless) — fall through to wait/kill.
        }

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
            await _process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Emit("Server did not exit gracefully; killing.", ConsoleEntrySeverity.Warning);
            Kill();
        }
    }

    public void Kill()
    {
        try
        {
            if (_process is { HasExited: false })
                _process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            Emit($"Failed to kill server: {ex.Message}", ConsoleEntrySeverity.Error);
        }
    }

    private void OnExited(object? sender, EventArgs e)
    {
        Emit("Server process exited.", ConsoleEntrySeverity.Warning);
        StartTime = null;
        RunningChanged?.Invoke(this, false);
        _process = null;
    }

    private void Emit(string text, ConsoleEntrySeverity severity)
        => OutputReceived?.Invoke(this, new ConsoleEntry(text, severity));
}
