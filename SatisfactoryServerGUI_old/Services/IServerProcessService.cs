using System;
using System.Threading.Tasks;
using SatisfactoryServerGUI.Models;

namespace SatisfactoryServerGUI.Services;

public interface IServerProcessService
{
    event EventHandler<ConsoleEntry>? OutputReceived;
    event EventHandler<bool>? RunningChanged;

    bool IsRunning { get; }
    DateTime? StartTime { get; }

    Task<bool> StartAsync(string exePath, string arguments, string? workingDir = null);
    Task StopAsync();
    void Kill();
}
