using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Models;
using SatisfactoryServerGUI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SatisfactoryServerGUI.ViewModels;

public partial class ConsoleLogsViewModel : ViewModelBase
{
    private ISettingsService _settings = null!;
    private readonly IServerProcessService _processService;
    private Timer? _fileCheckTimer;
    private string _factoryLogPath = "";
    private string _steamLogPath = "";
    private long _factoryLogLength;
    private long _steamLogLength;
    private readonly StringBuilder _factoryLogBuilder = new();
    private readonly StringBuilder _steamLogBuilder = new();

    [ObservableProperty]
    private string _satisfactoryLogText = "";

    [ObservableProperty]
    private string _steamCmdLogText = "";

    [ObservableProperty]
    private LogChoice _selectedLog = LogChoice.Satisfactory;

    [ObservableProperty]
    private FactoryLogPrefix _selectedFilter = FactoryLogPrefix.All;

    [ObservableProperty]
    private bool _autoScroll = true;

    public Array LogChoices => Enum.GetValues(typeof(LogChoice));
    public Array LogFilters => Enum.GetValues(typeof(FactoryLogPrefix));

    [ObservableProperty]
    private bool _showSatisfactoryLog = true;

    [ObservableProperty]
    private bool _showSteamCmdLog;

    partial void OnSelectedLogChanged(LogChoice value)
    {
        ShowSatisfactoryLog = value == LogChoice.Satisfactory;
        ShowSteamCmdLog = value == LogChoice.SteamCmd;
    }

    partial void OnSelectedFilterChanged(FactoryLogPrefix value)
    {
        if (SelectedLog == LogChoice.Satisfactory)
        {
            ApplyFilter();
        }
    }

    public ConsoleLogsViewModel(IServerProcessService processService)
    {
        _processService = processService;
        _processService.OutputReceived += OnProcessOutput;
    }

    public void Initialize(ISettingsService settings)
    {
        _settings = settings;
        UpdateLogPaths();

        _settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ISettingsService.ServerPath))
            {
                UpdateLogPaths();
            }
        };

        StartFileWatcher();
    }

    private void UpdateLogPaths()
    {
        if (string.IsNullOrEmpty(_settings.ServerPath)) return;

        _factoryLogPath = Path.Combine(_settings.ServerPath, "satisfactorydedicatedserver", "FactoryGame", "Saved", "Logs", "FactoryGame.log");
        _steamLogPath = Path.Combine(_settings.ServerPath, "steamCMD.log");

        _factoryLogLength = 0;
        _steamLogLength = 0;
        _factoryLogBuilder.Clear();
        _steamLogBuilder.Clear();

        // Initial load
        _ = RefreshLogsAsync();
    }

    private void OnProcessOutput(object? sender, string output)
    {
        // Add process output to the in-memory log
        _factoryLogBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {output}");

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (SelectedLog == LogChoice.Satisfactory)
            {
                ApplyFilter();
            }
        });
    }

    private void StartFileWatcher()
    {
        _fileCheckTimer = new Timer(2000);
        _fileCheckTimer.Elapsed += async (_, _) => await RefreshLogsAsync();
        _fileCheckTimer.Start();
    }

    private async Task RefreshLogsAsync()
    {
        try
        {
            await RefreshFactoryLogAsync();
            await RefreshSteamLogAsync();
        }
        catch
        {
            // File may be locked, try again next tick
        }
    }

    private async Task RefreshFactoryLogAsync()
    {
        if (!File.Exists(_factoryLogPath)) return;

        var info = new FileInfo(_factoryLogPath);
        if (info.Length <= _factoryLogLength) return;

        using var stream = new FileStream(_factoryLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(_factoryLogLength, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var newContent = await reader.ReadToEndAsync();
        _factoryLogLength = info.Length;

        if (!string.IsNullOrEmpty(newContent))
        {
            _factoryLogBuilder.Append(newContent);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (SelectedLog == LogChoice.Satisfactory)
                {
                    ApplyFilter();
                }
            });
        }
    }

    private async Task RefreshSteamLogAsync()
    {
        if (!File.Exists(_steamLogPath)) return;

        var info = new FileInfo(_steamLogPath);
        if (info.Length <= _steamLogLength) return;

        using var stream = new FileStream(_steamLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(_steamLogLength, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var newContent = await reader.ReadToEndAsync();
        _steamLogLength = info.Length;

        if (!string.IsNullOrEmpty(newContent))
        {
            _steamLogBuilder.Append(newContent);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (SelectedLog == LogChoice.SteamCmd)
                {
                    SteamCmdLogText = _steamLogBuilder.ToString();
                }
            });
        }
    }

    private void ApplyFilter()
    {
        var fullLog = _factoryLogBuilder.ToString();

        if (SelectedFilter == FactoryLogPrefix.All)
        {
            SatisfactoryLogText = fullLog;
        }
        else
        {
            var filter = SelectedFilter.ToString();
            var lines = fullLog.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var filtered = lines.Where(l => l.Contains(filter, StringComparison.OrdinalIgnoreCase));
            SatisfactoryLogText = string.Join(Environment.NewLine, filtered);
        }
    }

    public void Cleanup()
    {
        _fileCheckTimer?.Stop();
        _fileCheckTimer?.Dispose();
    }
}
