using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SatisfactoryServerGUI.Models;
using SatisfactoryServerGUI.Services;

namespace SatisfactoryServerGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly ISatisfactoryClientService _client;

    public ProfilePaneViewModel ProfilePane { get; }
    public AuthViewModel Auth { get; }
    public DashboardViewModel Dashboard { get; }
    public ConsoleViewModel Console { get; }
    public SavesViewModel Saves { get; }
    public SettingsViewModel Settings { get; }
    public LocalServerViewModel LocalServer { get; }

    [ObservableProperty]
    private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;

    [ObservableProperty]
    private string _connectionText = "Disconnected";

    [ObservableProperty]
    private bool _isLocalServerTabVisible;

    public MainWindowViewModel(
        ISettingsService settings,
        ISatisfactoryClientService client,
        ProfilePaneViewModel profilePane,
        AuthViewModel auth,
        DashboardViewModel dashboard,
        ConsoleViewModel console,
        SavesViewModel saves,
        SettingsViewModel settingsVm,
        LocalServerViewModel localServer)
    {
        _settings = settings;
        _client = client;
        ProfilePane = profilePane;
        Auth = auth;
        Dashboard = dashboard;
        Console = console;
        Saves = saves;
        Settings = settingsVm;
        LocalServer = localServer;

        _client.StatusChanged += OnStatusChanged;
        _settings.ActiveProfileChanged += OnActiveProfileChanged;
    }

    public async Task InitializeAsync()
    {
        await _settings.LoadAsync();
        ProfilePane.LoadProfiles();
        OnActiveProfileChanged(this, _settings.ActiveProfile);
    }

    public void OnShutdown()
    {
        try
        {
            // Best-effort persist on close.
            _settings.SaveAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // ignore
        }
    }

    private void OnStatusChanged(object? sender, ConnectionStatus status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ConnectionStatus = status;
            ConnectionText = status switch
            {
                ConnectionStatus.Connected => "Connected",
                ConnectionStatus.Connecting => "Connecting...",
                ConnectionStatus.Error => "Connection error",
                _ => "Disconnected",
            };
        });
    }

    private void OnActiveProfileChanged(object? sender, ServerProfile? profile)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsLocalServerTabVisible = profile?.IsLocallyManaged ?? false;
        });
    }
}
