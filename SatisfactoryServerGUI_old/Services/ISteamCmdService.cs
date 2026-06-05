using System;
using System.Threading;
using System.Threading.Tasks;
using SatisfactoryServerGUI.Models;

namespace SatisfactoryServerGUI.Services;

public interface ISteamCmdService
{
    event EventHandler<ConsoleEntry>? OutputReceived;

    bool IsBusy { get; }

    Task<bool> InstallOrUpdateAsync(
        string steamCmdPath,
        string installDir,
        string betaBranch = "public",
        CancellationToken ct = default);
}
