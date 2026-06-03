using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SatisfactoryServerGUI.Services;

public class ServerProcessService : IServerProcessService
{
    private Process? _process;
    private readonly object _lock = new();

    public event EventHandler<string>? OutputReceived;
    public event EventHandler? ProcessExited;
    public event EventHandler<bool>? StatusChanged;

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _process != null && !_process.HasExited;
            }
        }
    }

    public DateTime? StartTime { get; private set; }

    public Task<bool> StartAsync(string exePath, string arguments)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                if (_process != null && !_process.HasExited)
                    return false;

                if (!File.Exists(exePath))
                    return false;

                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

                var psi = new ProcessStartInfo();

                if (isWindows)
                {
                    psi.FileName = exePath;
                    psi.Arguments = arguments;
                }
                else if (isLinux)
                {
                    // On Linux, use the .sh script or run through SteamCMD
                    if (exePath.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))
                    {
                        psi.FileName = "/bin/bash";
                        psi.Arguments = $"\"{exePath}\" {arguments}";
                    }
                    else
                    {
                        psi.FileName = exePath;
                        psi.Arguments = arguments;
                    }
                }
                else
                {
                    return false;
                }

                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.WorkingDirectory = Path.GetDirectoryName(exePath) ?? "";

                _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                _process.OutputDataReceived += OnOutputDataReceived;
                _process.ErrorDataReceived += OnErrorDataReceived;
                _process.Exited += OnProcessExited;

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                StartTime = DateTime.Now;
                StatusChanged?.Invoke(this, true);
                return true;
            }
        });
    }

    public async Task StopAsync()
    {
        lock (_lock)
        {
            if (_process == null || _process.HasExited)
                return;

            try
            {
                _process.CloseMainWindow();
            }
            catch { }
        }

        // Wait up to 5 seconds for graceful shutdown
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(250);
            lock (_lock)
            {
                if (_process == null || _process.HasExited)
                    return;
            }
        }

        // Force kill if still running
        Kill();
    }

    public async Task RestartAsync(string exePath, string arguments)
    {
        await StopAsync();
        await StartAsync(exePath, arguments);
    }

    public void Kill()
    {
        lock (_lock)
        {
            if (_process == null || _process.HasExited)
                return;

            try
            {
                _process.Kill(true);
                _process.WaitForExit(5000);
            }
            catch { }
            finally
            {
                CleanupProcess();
            }
        }
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
            OutputReceived?.Invoke(this, e.Data);
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
            OutputReceived?.Invoke(this, $"[ERROR] {e.Data}");
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        lock (_lock)
        {
            CleanupProcess();
        }
        ProcessExited?.Invoke(this, EventArgs.Empty);
        StatusChanged?.Invoke(this, false);
    }

    private void CleanupProcess()
    {
        if (_process == null) return;

        try
        {
            _process.OutputDataReceived -= OnOutputDataReceived;
            _process.ErrorDataReceived -= OnErrorDataReceived;
            _process.Exited -= OnProcessExited;
            _process.Dispose();
        }
        catch { }
        finally
        {
            _process = null;
            StartTime = null;
        }
    }
}
