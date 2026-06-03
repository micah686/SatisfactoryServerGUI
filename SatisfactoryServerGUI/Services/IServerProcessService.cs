using System;
using System.Threading.Tasks;

namespace SatisfactoryServerGUI.Services;

public interface IServerProcessService
{
    event EventHandler<string>? OutputReceived;
    event EventHandler? ProcessExited;
    event EventHandler<bool>? StatusChanged;
    bool IsRunning { get; }
    DateTime? StartTime { get; }
    Task<bool> StartAsync(string exePath, string arguments);
    Task StopAsync();
    Task RestartAsync(string exePath, string arguments);
    void Kill();
}
