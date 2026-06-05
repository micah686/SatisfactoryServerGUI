using System;
using System.Threading;
using System.Threading.Tasks;
using SatisfactorySdk.DTO;

namespace SatisfactoryServerGUI.Services;

/// <summary>
/// Background polling of the HTTPS QueryServerState and the lightweight UDP query API.
/// Uses PeriodicTimer loops on background tasks (no UI-thread network I/O); consumers
/// marshal the raised events onto the UI thread themselves.
/// </summary>
public class ConnectionService : IConnectionService
{
    private readonly ISatisfactoryClientService _client;
    private CancellationTokenSource? _cts;

    public ConnectionService(ISatisfactoryClientService client)
    {
        _client = client;
    }

    public event EventHandler<QueryServerStateResponse?>? StatePolled;
    public event EventHandler<ServerStateResponse?>? UdpPolled;

    public bool IsPolling => _cts is { IsCancellationRequested: false };

    public void Start(TimeSpan httpsInterval, TimeSpan udpInterval)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _ = PollHttpsLoop(httpsInterval, _cts.Token);
        _ = PollUdpLoop(udpInterval, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task PollHttpsLoop(TimeSpan interval, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                var response = await _client.QueryServerStateAsync().ConfigureAwait(false);
                StatePolled?.Invoke(this, response.Ok() ? response.RequestResponse?.Data : null);
            }
            catch
            {
                StatePolled?.Invoke(this, null);
            }
        }
        while (await SafeWait(timer, ct).ConfigureAwait(false));
    }

    private async Task PollUdpLoop(TimeSpan interval, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                var response = await _client.PollServerStateUdpAsync().ConfigureAwait(false);
                UdpPolled?.Invoke(this, response);
            }
            catch
            {
                UdpPolled?.Invoke(this, null);
            }
        }
        while (await SafeWait(timer, ct).ConfigureAwait(false));
    }

    private static async Task<bool> SafeWait(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
