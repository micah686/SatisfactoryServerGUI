using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SatisfactoryServerGUI.Services;

public partial class SettingsService : ObservableObject, ISettingsService
{
    private readonly string _settingsPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ServerExePath))]
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
    private int _beaconPort;

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private string? _multiHome;

    [ObservableProperty]
    private bool _disablePacketRouting;

    [ObservableProperty]
    private bool _disableSeasonalEvents;

    [ObservableProperty]
    private bool _noSteam;

    [ObservableProperty]
    private bool _noVisibleConsole = true;

    [ObservableProperty]
    private string _serverVersion = "Public";

    public string ServerExePath =>
        Path.Combine(ServerPath, "satisfactorydedicatedserver", "FactoryServer.exe");

    public string ServerShPath =>
        Path.Combine(ServerPath, "satisfactorydedicatedserver", "FactoryServer.sh");

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "SatisfactoryServerGUI");
        Directory.CreateDirectory(configDir);
        _settingsPath = Path.Combine(configDir, "settings.json");
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_settingsPath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            var data = JsonSerializer.Deserialize<SettingsData>(json);
            if (data == null) return;

            ServerPath = data.ServerPath ?? "";
            ServerIp = data.ServerIp ?? "127.0.0.1";
            ServerApiPort = data.ServerApiPort;
            AuthToken = data.AuthToken;
            TrustSelfSignedCerts = data.TrustSelfSignedCerts;
            ServerQueryPort = data.ServerQueryPort;
            BeaconPort = data.BeaconPort;
            Port = data.Port;
            MultiHome = data.MultiHome;
            DisablePacketRouting = data.DisablePacketRouting;
            DisableSeasonalEvents = data.DisableSeasonalEvents;
            NoSteam = data.NoSteam;
            NoVisibleConsole = data.NoVisibleConsole;
            ServerVersion = data.ServerVersion ?? "Public";
        }
        catch
        {
            // Use defaults if settings file is corrupted
        }
    }

    public async Task SaveAsync()
    {
        var data = new SettingsData
        {
            ServerPath = ServerPath,
            ServerIp = ServerIp,
            ServerApiPort = ServerApiPort,
            AuthToken = AuthToken,
            TrustSelfSignedCerts = TrustSelfSignedCerts,
            ServerQueryPort = ServerQueryPort,
            BeaconPort = BeaconPort,
            Port = Port,
            MultiHome = MultiHome,
            DisablePacketRouting = DisablePacketRouting,
            DisableSeasonalEvents = DisableSeasonalEvents,
            NoSteam = NoSteam,
            NoVisibleConsole = NoVisibleConsole,
            ServerVersion = ServerVersion
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    private class SettingsData
    {
        public string? ServerPath { get; set; }
        public string? ServerIp { get; set; }
        public int ServerApiPort { get; set; }
        public string? AuthToken { get; set; }
        public bool TrustSelfSignedCerts { get; set; }
        public int ServerQueryPort { get; set; }
        public int BeaconPort { get; set; }
        public int Port { get; set; }
        public string? MultiHome { get; set; }
        public bool DisablePacketRouting { get; set; }
        public bool DisableSeasonalEvents { get; set; }
        public bool NoSteam { get; set; }
        public bool NoVisibleConsole { get; set; } = true;
        public string? ServerVersion { get; set; }
    }
}
