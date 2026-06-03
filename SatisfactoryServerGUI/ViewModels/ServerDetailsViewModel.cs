using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Services;
using SatisfactorySdk.DTO;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;

namespace SatisfactoryServerGUI.ViewModels;

public partial class ServerDetailsViewModel : ViewModelBase
{
    private readonly ISatisfactoryClientService _clientService;
    private ISettingsService _settings = null!;
    private Timer? _refreshTimer;

    [ObservableProperty]
    private string _externalIp = "Unknown";

    [ObservableProperty]
    private string _internalIp = "Unknown";

    [ObservableProperty]
    private string _serverName = "Not connected";

    [ObservableProperty]
    private int _connectedPlayers;

    [ObservableProperty]
    private int _playerLimit;

    [ObservableProperty]
    private string _activeSessionName = "-";

    [ObservableProperty]
    private string _gamePhase = "-";

    [ObservableProperty]
    private bool _isGameRunning;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private float _averageTickRate;

    [ObservableProperty]
    private int _techTier;

    [ObservableProperty]
    private bool _isApiConnected;

    [ObservableProperty]
    private string _apiStatus = "Not configured";

    [ObservableProperty]
    private string _lastSaveTime = "-";

    [ObservableProperty]
    private string _gameStatusText = "Not running";

    [ObservableProperty]
    private bool _isAutoRefresh = true;

    partial void OnIsGameRunningChanged(bool value)
    {
        UpdateGameStatusText();
    }

    partial void OnIsPausedChanged(bool value)
    {
        UpdateGameStatusText();
    }

    private void UpdateGameStatusText()
    {
        var status = IsGameRunning ? "Running" : "Not running";
        var paused = IsPaused ? "Paused" : "Unpaused";
        GameStatusText = $"{status} ({paused})";
    }

    public ServerDetailsViewModel(ISatisfactoryClientService clientService)
    {
        _clientService = clientService;
    }

    public void Initialize(ISettingsService settings)
    {
        _settings = settings;
        LoadIps();
        ConfigureApiClient();

        _settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ISettingsService.ServerIp) ||
                e.PropertyName == nameof(ISettingsService.ServerApiPort) ||
                e.PropertyName == nameof(ISettingsService.AuthToken) ||
                e.PropertyName == nameof(ISettingsService.TrustSelfSignedCerts))
            {
                ConfigureApiClient();
            }
        };

        if (IsAutoRefresh)
            StartAutoRefresh();
    }

    partial void OnIsAutoRefreshChanged(bool value)
    {
        if (value)
            StartAutoRefresh();
        else
            StopAutoRefresh();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        LoadIps();
        await RefreshServerStateAsync();
    }

    [RelayCommand]
    private Task RefreshIpsAsync()
    {
        LoadIps();
        return Task.CompletedTask;
    }

    private void ConfigureApiClient()
    {
        if (string.IsNullOrEmpty(_settings.ServerIp) || _settings.ServerApiPort <= 0)
        {
            ApiStatus = "Not configured";
            return;
        }

        _clientService.Configure(
            _settings.ServerIp,
            _settings.ServerApiPort,
            _settings.AuthToken,
            _settings.TrustSelfSignedCerts);

        ApiStatus = "Configured";
        _ = RefreshServerStateAsync();
    }

    private async Task RefreshServerStateAsync()
    {
        if (!_clientService.IsConfigured) return;

        try
        {
            var healthResult = await _clientService.HealthCheckAsync("SatisfactoryServerGUI");
            IsApiConnected = healthResult.IsSuccessful;
            ApiStatus = healthResult.IsSuccessful ? "Connected" : "Health check failed";

            if (!healthResult.IsSuccessful) return;

            var stateResult = await _clientService.QueryServerStateAsync();
            if (stateResult.IsSuccessful && stateResult.RequestResponse?.Data != null)
            {
                var state = stateResult.RequestResponse.Data.ServerState;
                ServerName = state.ActiveSessionName ?? "Unknown";
                ConnectedPlayers = state.ConnectedPlayers;
                PlayerLimit = state.PlayerLimit;
                ActiveSessionName = state.ActiveSessionName ?? "-";
                GamePhase = state.GamePhase ?? "-";
                IsGameRunning = state.IsGameRunning;
                IsPaused = state.IsPaused;
                AverageTickRate = state.AverageTickRate;
                TechTier = state.TechTier;
            }

            var sessionsResult = await _clientService.EnumerateSessionsAsync();
            if (sessionsResult.IsSuccessful && sessionsResult.RequestResponse?.Data != null)
            {
                var current = sessionsResult.RequestResponse.Data.CurrentSession;
                if (current?.SaveHeaders?.Count > 0)
                {
                    var lastSave = current.SaveHeaders[0];
                    LastSaveTime = lastSave.SaveDate.ToString("g");
                }
            }
        }
        catch (Exception ex)
        {
            IsApiConnected = false;
            ApiStatus = $"Error: {ex.Message}";
        }
    }

    private void LoadIps()
    {
        // Internal IP
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                InternalIp = endPoint.Address.ToString();
            }
            socket.Close();
        }
        catch
        {
            InternalIp = "Unable to determine";
        }

        // External IP - fire and forget
        _ = Task.Run(async () =>
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var ip = await client.GetStringAsync("https://api.ipify.org");
                ExternalIp = ip.Trim();
            }
            catch
            {
                ExternalIp = "Unable to determine";
            }
        });
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();
        _refreshTimer = new Timer(5000);
        _refreshTimer.Elapsed += async (_, _) => await RefreshServerStateAsync();
        _refreshTimer.Start();
    }

    private void StopAutoRefresh()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }
}
