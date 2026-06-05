using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Models;
using SatisfactoryServerGUI.Services;

namespace SatisfactoryServerGUI.ViewModels;

public partial class LocalServerViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IServerProcessService _process;
    private readonly ISteamCmdService _steam;
    private readonly IDialogService _dialogs;
    private readonly DispatcherTimer _uptimeTimer;
    private CancellationTokenSource? _steamCts;

    public ObservableCollection<ConsoleEntry> Output { get; } = new();

    [ObservableProperty] private string _installDir = string.Empty;
    [ObservableProperty] private string _steamCmdPath = string.Empty;
    [ObservableProperty] private string _betaBranch = "public";
    [ObservableProperty] private int _gamePort = 7777;
    [ObservableProperty] private int _queryPort = 7777;
    [ObservableProperty] private int _beaconPort = 15000;
    [ObservableProperty] private string _multiHome = string.Empty;
    [ObservableProperty] private string _extraArgs = string.Empty;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private string _uptime = "—";

    public LocalServerViewModel(
        ISettingsService settings,
        IServerProcessService process,
        ISteamCmdService steam,
        IDialogService dialogs)
    {
        _settings = settings;
        _process = process;
        _steam = steam;
        _dialogs = dialogs;

        _process.OutputReceived += (_, e) => Append(e);
        _steam.OutputReceived += (_, e) => Append(e);
        _process.RunningChanged += (_, running) => Dispatcher.UIThread.Post(() => IsRunning = running);

        _settings.ActiveProfileChanged += (_, profile) => Dispatcher.UIThread.Post(() => LoadFrom(profile));

        _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uptimeTimer.Tick += (_, _) => UpdateUptime();
        _uptimeTimer.Start();

        LoadFrom(_settings.ActiveProfile);
    }

    private void LoadFrom(ServerProfile? profile)
    {
        var local = profile?.Local ?? new LocalServerSettings();
        InstallDir = local.InstallDir;
        SteamCmdPath = local.SteamCmdPath;
        BetaBranch = local.BetaBranch;
        GamePort = local.GamePort;
        QueryPort = local.QueryPort;
        BeaconPort = local.BeaconPort;
        MultiHome = local.MultiHome ?? string.Empty;
        ExtraArgs = local.ExtraArgs;
    }

    private LocalServerSettings BuildSettings() => new()
    {
        InstallDir = InstallDir,
        SteamCmdPath = SteamCmdPath,
        BetaBranch = BetaBranch,
        GamePort = GamePort,
        QueryPort = QueryPort,
        BeaconPort = BeaconPort,
        MultiHome = string.IsNullOrWhiteSpace(MultiHome) ? null : MultiHome,
        ExtraArgs = ExtraArgs,
    };

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (_settings.ActiveProfile is not { } profile)
        {
            SetStatus("No active profile.", isError: true);
            return;
        }

        profile.Local = BuildSettings();
        profile.IsLocallyManaged = true;
        _settings.UpdateProfile(profile);
        await _settings.SaveAsync();
        SetStatus("Local server settings saved.");
    }

    [RelayCommand]
    private async Task PickInstallDirAsync()
    {
        var dir = await _dialogs.PickFolderAsync("Select install directory");
        if (!string.IsNullOrWhiteSpace(dir)) InstallDir = dir;
    }

    [RelayCommand]
    private async Task PickSteamCmdAsync()
    {
        var file = await _dialogs.PickFileAsync("Select steamcmd executable");
        if (!string.IsNullOrWhiteSpace(file)) SteamCmdPath = file;
    }

    [RelayCommand]
    private async Task InstallOrUpdateAsync()
    {
        IsInstalling = true;
        _steamCts = new CancellationTokenSource();
        try
        {
            await _steam.InstallOrUpdateAsync(SteamCmdPath, InstallDir, BetaBranch, _steamCts.Token);
        }
        finally
        {
            IsInstalling = false;
            _steamCts?.Dispose();
            _steamCts = null;
        }
    }

    [RelayCommand]
    private void CancelInstall() => _steamCts?.Cancel();

    [RelayCommand]
    private async Task StartAsync()
    {
        var settings = BuildSettings();
        var exe = settings.ResolveServerExecutable();
        await _process.StartAsync(exe, settings.BuildLaunchArgs());
    }

    [RelayCommand]
    private async Task StopAsync() => await _process.StopAsync();

    [RelayCommand]
    private async Task RestartAsync()
    {
        await _process.StopAsync();
        var settings = BuildSettings();
        await _process.StartAsync(settings.ResolveServerExecutable(), settings.BuildLaunchArgs());
    }

    [RelayCommand]
    private void Kill() => _process.Kill();

    [RelayCommand]
    private void ClearOutput() => Output.Clear();

    private void Append(ConsoleEntry entry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Output.Add(entry);
            if (Output.Count > 2000)
                Output.RemoveAt(0);
        });
    }

    private void UpdateUptime()
    {
        if (_process.IsRunning && _process.StartTime is { } start)
        {
            var span = DateTime.Now - start;
            Uptime = $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
        }
        else
        {
            Uptime = "—";
        }
    }
}
