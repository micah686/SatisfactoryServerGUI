using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SatisfactoryServerGUI.ViewModels;

public partial class ServerSettingsViewModel : ViewModelBase
{
    private ISettingsService _settings = null!;

    [ObservableProperty]
    private string _serverPath = "";

    [ObservableProperty]
    private string _serverIp = "127.0.0.1";

    [ObservableProperty]
    private int _serverApiPort = 7777;

    [ObservableProperty]
    private string? _authToken;

    [ObservableProperty]
    private bool _trustSelfSignedCerts = true;

    [ObservableProperty]
    private int _serverQueryPort;

    [ObservableProperty]
    private bool _isServerQueryPortEnabled;

    [ObservableProperty]
    private int _beaconPort;

    [ObservableProperty]
    private bool _isBeaconPortEnabled;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private bool _isPortEnabled;

    [ObservableProperty]
    private string _multiHome = "";

    [ObservableProperty]
    private bool _isMultiHomeEnabled;

    [ObservableProperty]
    private bool _disablePacketRouting;

    [ObservableProperty]
    private bool _disableSeasonalEvents;

    [ObservableProperty]
    private string _statusMessage = "";

    public void Initialize(ISettingsService settings)
    {
        _settings = settings;

        ServerPath = settings.ServerPath;
        ServerIp = settings.ServerIp;
        ServerApiPort = settings.ServerApiPort;
        AuthToken = settings.AuthToken;
        TrustSelfSignedCerts = settings.TrustSelfSignedCerts;

        IsServerQueryPortEnabled = settings.ServerQueryPort > 0;
        ServerQueryPort = settings.ServerQueryPort > 0 ? settings.ServerQueryPort : 0;

        IsBeaconPortEnabled = settings.BeaconPort > 0;
        BeaconPort = settings.BeaconPort > 0 ? settings.BeaconPort : 0;

        IsPortEnabled = settings.Port > 0;
        Port = settings.Port > 0 ? settings.Port : 0;

        IsMultiHomeEnabled = !string.IsNullOrEmpty(settings.MultiHome);
        MultiHome = settings.MultiHome ?? "";

        DisablePacketRouting = settings.DisablePacketRouting;
        DisableSeasonalEvents = settings.DisableSeasonalEvents;
    }

    [RelayCommand]
    private async Task ChooseServerPathAsync()
    {
        if (App.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var options = new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "Choose Server Installation Path",
            AllowMultiple = false
        };

        var result = await desktop.MainWindow!.StorageProvider.OpenFolderPickerAsync(options);
        if (result.Count > 0 && result[0].Path.LocalPath is { } path)
        {
            ServerPath = path;
            _settings.ServerPath = path;
            await SaveSettingsAsync();
            StatusMessage = "Server path updated";
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        _settings.ServerPath = ServerPath;
        _settings.ServerIp = ServerIp;
        _settings.ServerApiPort = ServerApiPort;
        _settings.AuthToken = AuthToken;
        _settings.TrustSelfSignedCerts = TrustSelfSignedCerts;

        _settings.ServerQueryPort = IsServerQueryPortEnabled ? ServerQueryPort : 0;
        _settings.BeaconPort = IsBeaconPortEnabled ? BeaconPort : 0;
        _settings.Port = IsPortEnabled ? Port : 0;
        _settings.MultiHome = IsMultiHomeEnabled ? MultiHome : null;

        if (IsMultiHomeEnabled && !string.IsNullOrEmpty(MultiHome) && !IsValidIp(MultiHome))
        {
            MultiHome = "127.0.0.1";
            _settings.MultiHome = "127.0.0.1";
        }

        _settings.DisablePacketRouting = DisablePacketRouting;
        _settings.DisableSeasonalEvents = DisableSeasonalEvents;

        await _settings.SaveAsync();
        StatusMessage = "Settings saved successfully";
    }

    partial void OnIsMultiHomeEnabledChanged(bool value)
    {
        if (!value)
        {
            DisablePacketRouting = false;
        }
    }

    partial void OnMultiHomeChanged(string value)
    {
        // Filter to digits and dots only for IP
        var filtered = new string(value.Where(c => char.IsDigit(c) || c == '.').ToArray());
        if (filtered != value)
        {
            MultiHome = filtered;
        }
    }

    private static bool IsValidIp(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return false;
        var parts = ip.Split('.');
        if (parts.Length != 4) return false;
        return parts.All(p => byte.TryParse(p, out _));
    }
}
