using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Services;
using System;
using System.Threading.Tasks;

namespace SatisfactoryServerGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _statusText = "Stopped";

    [ObservableProperty]
    private bool _isServerRunning;

    [ObservableProperty]
    private string _uptimeText = "";

    [ObservableProperty]
    private ServerControlsViewModel _serverControlsViewModel;

    [ObservableProperty]
    private ServerDetailsViewModel _serverDetailsViewModel;

    [ObservableProperty]
    private ServerFoldersViewModel _serverFoldersViewModel;

    [ObservableProperty]
    private ServerSettingsViewModel _serverSettingsViewModel;

    [ObservableProperty]
    private ConsoleLogsViewModel _consoleLogsViewModel;

    private System.Timers.Timer? _uptimeTimer;
    private DateTime? _serverStartTime;

    public MainWindowViewModel(
        ISettingsService settingsService,
        IServerProcessService processService,
        ISatisfactoryClientService clientService,
        ServerControlsViewModel serverControlsVm,
        ServerDetailsViewModel serverDetailsVm,
        ServerFoldersViewModel serverFoldersVm,
        ServerSettingsViewModel serverSettingsVm,
        ConsoleLogsViewModel consoleLogsVm)
    {
        _settingsService = settingsService;
        ServerControlsViewModel = serverControlsVm;
        ServerDetailsViewModel = serverDetailsVm;
        ServerFoldersViewModel = serverFoldersVm;
        ServerSettingsViewModel = serverSettingsVm;
        ConsoleLogsViewModel = consoleLogsVm;

        processService.StatusChanged += OnServerStatusChanged;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await _settingsService.LoadAsync();
        ServerControlsViewModel.Initialize(_settingsService);
        ServerDetailsViewModel.Initialize(_settingsService);
        ServerFoldersViewModel.Initialize(_settingsService);
        ServerSettingsViewModel.Initialize(_settingsService);
        ConsoleLogsViewModel.Initialize(_settingsService);
    }

    private void OnServerStatusChanged(object? sender, bool isRunning)
    {
        IsServerRunning = isRunning;
        StatusText = isRunning ? "Running" : "Stopped";

        if (isRunning)
        {
            _serverStartTime = DateTime.Now;
            StartUptimeTimer();
        }
        else
        {
            _serverStartTime = null;
            StopUptimeTimer();
            UptimeText = "";
        }
    }

    private void StartUptimeTimer()
    {
        _uptimeTimer ??= new System.Timers.Timer(1000);
        _uptimeTimer.Elapsed += (_, _) =>
        {
            if (_serverStartTime.HasValue)
            {
                var elapsed = DateTime.Now - _serverStartTime.Value;
                var parts = new System.Collections.Generic.List<string>();
                if (elapsed.Days > 0) parts.Add($"{elapsed.Days}d");
                if (elapsed.Hours > 0) parts.Add($"{elapsed.Hours}h");
                if (elapsed.Minutes > 0) parts.Add($"{elapsed.Minutes}m");
                parts.Add($"{elapsed.Seconds}s");
                UptimeText = string.Join(" ", parts);
            }
        };
        _uptimeTimer.Start();
    }

    private void StopUptimeTimer()
    {
        _uptimeTimer?.Stop();
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await _settingsService.SaveAsync();
    }
}
