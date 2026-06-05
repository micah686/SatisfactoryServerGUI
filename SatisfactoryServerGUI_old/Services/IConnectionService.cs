using System;
using SatisfactorySdk.DTO;

namespace SatisfactoryServerGUI.Services;

public interface IConnectionService
{
    event EventHandler<QueryServerStateResponse?>? StatePolled;
    event EventHandler<ServerStateResponse?>? UdpPolled;

    bool IsPolling { get; }

    void Start(TimeSpan httpsInterval, TimeSpan udpInterval);
    void Stop();
}
