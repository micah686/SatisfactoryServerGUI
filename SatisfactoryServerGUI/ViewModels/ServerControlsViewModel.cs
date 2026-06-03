using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Models;
using SatisfactoryServerGUI.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;

namespace SatisfactoryServerGUI.ViewModels;

public partial class ServerControlsViewModel : ViewModelBase
{
    private readonly IServerProcessService _processService;
    private ISettingsService _settings = null!;

    [ObservableProperty]
    private bool _isNoSteam;

    [ObservableProperty]
    private bool _isNoVisibleConsole = true;

    [ObservableProperty]
    private ServerVersion _selectedVersion = ServerVersion.Public;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isSteamCmdDownloading;

    [ObservableProperty]
    private string _steamCmdStatus = "";

    private Process? _steamCmdProcess;

    public ServerControlsViewModel(IServerProcessService processService)
    {
        _processService = processService;
        _processService.OutputReceived += OnProcessOutput;
        _processService.ProcessExited += OnProcessExited;
    }

    public void Initialize(ISettingsService settings)
    {
        _settings = settings;
        IsNoSteam = settings.NoSteam;
        IsNoVisibleConsole = settings.NoVisibleConsole;
        SelectedVersion = settings.ServerVersion switch
        {
            "Experimental" => ServerVersion.Experimental,
            _ => ServerVersion.Public
        };
    }

    partial void OnIsNoSteamChanged(bool value) => _settings.NoSteam = value;
    partial void OnIsNoVisibleConsoleChanged(bool value) => _settings.NoVisibleConsole = value;
    partial void OnSelectedVersionChanged(ServerVersion value) => _settings.ServerVersion = value.ToString();

    [RelayCommand]
    private async Task ToggleServerAsync()
    {
        if (_processService.IsRunning)
        {
            await StopServerAsync();
        }
        else
        {
            await StartServerAsync();
        }
    }

    [RelayCommand]
    private async Task StartServerAsync()
    {
        if (string.IsNullOrEmpty(_settings.ServerPath))
        {
            StatusMessage = "Server path not configured";
            return;
        }

        IsBusy = true;
        StatusMessage = "Starting server...";

        try
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var exePath = isWindows ? _settings.ServerExePath : _settings.ServerShPath;
            var arguments = BuildArguments();

            var success = await _processService.StartAsync(exePath, arguments);
            StatusMessage = success ? "Server started" : "Failed to start server";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task StopServerAsync()
    {
        IsBusy = true;
        StatusMessage = "Stopping server...";
        await _processService.StopAsync();
        StatusMessage = "Server stopped";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RestartServerAsync()
    {
        IsBusy = true;
        StatusMessage = "Restarting server...";
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var exePath = isWindows ? _settings.ServerExePath : _settings.ServerShPath;
        var arguments = BuildArguments();
        await _processService.RestartAsync(exePath, arguments);
        StatusMessage = "Server restarted";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task InstallUpdateServerAsync()
    {
        if (string.IsNullOrEmpty(_settings.ServerPath))
        {
            StatusMessage = "Server path not configured";
            return;
        }

        IsSteamCmdDownloading = true;
        SteamCmdStatus = "Downloading SteamCMD...";

        try
        {
            await DownloadSteamCmdAsync();
            SteamCmdStatus = "Installing/Updating Satisfactory Dedicated Server...";
            await RunSteamCmdAsync();
            SteamCmdStatus = "Installation complete!";
        }
        catch (Exception ex)
        {
            SteamCmdStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsSteamCmdDownloading = false;
        }
    }

    private async Task DownloadSteamCmdAsync()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        if (isWindows)
        {
            var filePath = Path.Combine(_settings.ServerPath, "steamcmd.exe");
            if (File.Exists(filePath)) return;

            using var httpClient = new HttpClient();
            var data = await httpClient.GetByteArrayAsync("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip");
            using var ms = new MemoryStream(data);
            using var archive = new ZipArchive(ms);
            archive.Entries[0].ExtractToFile(filePath, false);
        }
        else
        {
            var filePath = Path.Combine(_settings.ServerPath, "steamcmd.sh");
            if (File.Exists(filePath)) return;

            using var httpClient = new HttpClient();
            var data = await httpClient.GetByteArrayAsync("https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz");
            var tarPath = Path.Combine(_settings.ServerPath, "steamcmd.tar.gz");
            await File.WriteAllBytesAsync(tarPath, data);

            var psi = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xzf \"{tarPath}\" -C \"{_settings.ServerPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi);
            await proc!.WaitForExitAsync();
            File.Delete(tarPath);
        }
    }

    private async Task RunSteamCmdAsync()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var steamCmdPath = isWindows
            ? Path.Combine(_settings.ServerPath, "steamcmd.exe")
            : Path.Combine(_settings.ServerPath, "steamcmd.sh");

        var installDir = Path.Combine(_settings.ServerPath, "SatisfactoryDedicatedServer");
        var arguments = $"+login anonymous +force_install_dir \"{installDir}\" +app_update 1690800 -beta {SelectedVersion} validate +quit";

        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? steamCmdPath : "/bin/bash",
            Arguments = isWindows ? arguments : $"\"{steamCmdPath}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _settings.ServerPath
        };

        _steamCmdProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _steamCmdProcess.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                SteamCmdStatus = e.Data;
        };
        _steamCmdProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                SteamCmdStatus = $"[ERROR] {e.Data}";
        };

        _steamCmdProcess.Start();
        _steamCmdProcess.BeginOutputReadLine();
        _steamCmdProcess.BeginErrorReadLine();
        await _steamCmdProcess.WaitForExitAsync();
    }

    private string BuildArguments()
    {
        var args = new System.Collections.Generic.List<string>();

        if (IsNoSteam) args.Add("-nosteam");
        if (!IsNoVisibleConsole) args.Add("-log");

        args.Add($"-beta {SelectedVersion}");

        if (_settings.ServerQueryPort > 0)
            args.Add($"-ServerQueryPort={_settings.ServerQueryPort}");
        if (_settings.BeaconPort > 0)
            args.Add($"-BeaconPort={_settings.BeaconPort}");
        if (_settings.Port > 0)
            args.Add($"-Port={_settings.Port}");
        if (!string.IsNullOrEmpty(_settings.MultiHome))
            args.Add($"-multihome={_settings.MultiHome}");
        if (_settings.DisablePacketRouting)
            args.Add("-DisablePacketRouting");
        if (_settings.DisableSeasonalEvents)
            args.Add("-DisableSeasonalEvents");

        args.Add("-unattended");

        return string.Join(" ", args);
    }

    private void OnProcessOutput(object? sender, string output)
    {
        StatusMessage = output;
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        StatusMessage = "Server process exited";
    }
}
