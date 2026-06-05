using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Models;
using SatisfactoryServerGUI.Services;

namespace SatisfactoryServerGUI.ViewModels;

public partial class ConsoleViewModel : ViewModelBase
{
    private readonly ISatisfactoryClientService _client;
    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    public ObservableCollection<ConsoleEntry> Entries { get; } = new();

    [ObservableProperty]
    private string _currentCommand = string.Empty;

    public ConsoleViewModel(ISatisfactoryClientService client)
    {
        _client = client;
        Entries.Add(new ConsoleEntry("Console ready. Commands require an Admin token.", ConsoleEntrySeverity.Info));
    }

    [RelayCommand]
    private async Task RunCommandAsync()
    {
        var command = CurrentCommand.Trim();
        if (command.Length == 0)
            return;

        if (!_client.IsConfigured)
        {
            Entries.Add(new ConsoleEntry("Not connected.", ConsoleEntrySeverity.Error));
            return;
        }

        _history.Add(command);
        _historyIndex = _history.Count;
        Entries.Add(new ConsoleEntry($"> {command}", ConsoleEntrySeverity.Command));
        CurrentCommand = string.Empty;

        await RunBusyAsync(async () =>
        {
            var response = await _client.RunCommandAsync(command);
            if (response.Ok())
            {
                var result = response.RequestResponse?.Data?.CommandResult ?? string.Empty;
                Entries.Add(new ConsoleEntry(
                    string.IsNullOrWhiteSpace(result) ? "(no output)" : result,
                    ConsoleEntrySeverity.Response));
            }
            else
            {
                Entries.Add(new ConsoleEntry(response.ErrorText(), ConsoleEntrySeverity.Error));
            }
        }, "Command failed");
    }

    [RelayCommand]
    private void Clear() => Entries.Clear();

    [RelayCommand]
    private void HistoryPrevious()
    {
        if (_history.Count == 0) return;
        _historyIndex = System.Math.Max(0, _historyIndex - 1);
        CurrentCommand = _history[_historyIndex];
    }

    [RelayCommand]
    private void HistoryNext()
    {
        if (_history.Count == 0) return;
        if (_historyIndex >= _history.Count - 1)
        {
            _historyIndex = _history.Count;
            CurrentCommand = string.Empty;
            return;
        }

        _historyIndex++;
        CurrentCommand = _history[_historyIndex];
    }
}
