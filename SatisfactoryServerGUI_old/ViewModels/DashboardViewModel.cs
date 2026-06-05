using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactorySdk.DTO;
using SatisfactoryServerGUI.Services;

namespace SatisfactoryServerGUI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ISatisfactoryClientService _client;

    [ObservableProperty] private string _serverName = "—";
    [ObservableProperty] private string _udpState = "Unknown";
    [ObservableProperty] private string _activeSessionName = "—";
    [ObservableProperty] private int _connectedPlayers;
    [ObservableProperty] private int _playerLimit;
    [ObservableProperty] private int _techTier;
    [ObservableProperty] private string _gamePhase = "—";
    [ObservableProperty] private bool _isGameRunning;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private float _averageTickRate;
    [ObservableProperty] private int _totalGameDurationSeconds;
    [ObservableProperty] private string _autoLoadSessionName = "—";
    [ObservableProperty] private long _netChangelist;
    [ObservableProperty] private string _lastUpdated = "never";

    public DashboardViewModel(ISatisfactoryClientService client, IConnectionService connection)
    {
        _client = client;
        connection.StatePolled += OnStatePolled;
        connection.UdpPolled += OnUdpPolled;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            var response = await _client.QueryServerStateAsync();
            if (response.Ok())
            {
                ApplyHttpsState(response.RequestResponse?.Data);
                SetStatus("Refreshed.");
            }
            else
            {
                SetStatus($"Query failed: {response.ErrorText()}", isError: true);
            }
        }, "Refresh failed");
    }

    private void OnStatePolled(object? sender, QueryServerStateResponse? response)
        => Dispatcher.UIThread.Post(() => ApplyHttpsState(response));

    private void OnUdpPolled(object? sender, ServerStateResponse? response)
        => Dispatcher.UIThread.Post(() =>
        {
            if (response?.ServerState is { } udp)
            {
                ServerName = string.IsNullOrWhiteSpace(udp.ServerName) ? "—" : udp.ServerName;
                UdpState = udp.ServerState.ToString();
                NetChangelist = udp.ServerNetCL;
            }
        });

    private void ApplyHttpsState(QueryServerStateResponse? response)
    {
        if (response?.ServerState is not { } state)
            return;

        ActiveSessionName = string.IsNullOrWhiteSpace(state.ActiveSessionName) ? "—" : state.ActiveSessionName;
        ConnectedPlayers = state.ConnectedPlayers;
        PlayerLimit = state.PlayerLimit;
        TechTier = state.TechTier;
        GamePhase = string.IsNullOrWhiteSpace(state.GamePhase) ? "—" : state.GamePhase;
        IsGameRunning = state.IsGameRunning;
        IsPaused = state.IsPaused;
        AverageTickRate = state.AverageTickRate;
        TotalGameDurationSeconds = state.TotalGameDuration;
        AutoLoadSessionName = string.IsNullOrWhiteSpace(state.AutoLoadSessionName) ? "—" : state.AutoLoadSessionName;
        LastUpdated = DateTime.Now.ToString("HH:mm:ss");
    }
}
