using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Services;

namespace SatisfactoryServerGUI.ViewModels;

public partial class KeyValueRow : ObservableObject
{
    [ObservableProperty] private string _key = string.Empty;
    [ObservableProperty] private string _value = string.Empty;
}

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISatisfactoryClientService _client;
    private readonly IDialogService _dialogs;

    public ObservableCollection<KeyValueRow> ServerOptions { get; } = new();
    public ObservableCollection<KeyValueRow> AdvancedSettings { get; } = new();
    public ObservableCollection<KeyValueRow> PendingOptions { get; } = new();

    [ObservableProperty] private bool _isCreativeEnabled;
    [ObservableProperty] private string _newServerName = string.Empty;
    [ObservableProperty] private string _newClientPassword = string.Empty;
    [ObservableProperty] private string _newAdminPassword = string.Empty;

    public SettingsViewModel(ISatisfactoryClientService client, IDialogService dialogs)
    {
        _client = client;
        _dialogs = dialogs;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            var options = await _client.GetServerOptionsAsync();
            if (options.Ok() && options.RequestResponse?.Data is { } od)
            {
                Fill(ServerOptions, od.ServerOptions);
                Fill(PendingOptions, od.PendingServerOptions);
            }

            var adv = await _client.GetAdvancedGameSettingsAsync();
            if (adv.Ok() && adv.RequestResponse?.Data is { } ad)
            {
                Fill(AdvancedSettings, ad.AdvancedGameSettings);
                IsCreativeEnabled = ad.IsCreativeEnabled;
            }

            SetStatus("Settings loaded.");
        }, "Load settings failed");
    }

    [RelayCommand]
    private async Task ApplyServerOptionsAsync()
    {
        await RunBusyAsync(async () =>
        {
            var response = await _client.ApplyServerOptionsAsync(ToDictionary(ServerOptions));
            SetStatus(response.Ok() ? "Server options applied." : $"Failed: {response.ErrorText()}", !response.Ok());
        }, "Apply server options failed");
    }

    [RelayCommand]
    private async Task ApplyAdvancedSettingsAsync()
    {
        await RunBusyAsync(async () =>
        {
            var response = await _client.ApplyAdvancedGameSettingsAsync(ToDictionary(AdvancedSettings));
            SetStatus(response.Ok() ? "Advanced settings applied." : $"Failed: {response.ErrorText()}", !response.Ok());
        }, "Apply advanced settings failed");
    }

    [RelayCommand]
    private async Task RenameAsync()
    {
        if (string.IsNullOrWhiteSpace(NewServerName)) return;
        await RunBusyAsync(async () =>
        {
            var response = await _client.RenameServerAsync(NewServerName);
            SetStatus(response.Ok() ? "Server renamed." : $"Rename failed: {response.ErrorText()}", !response.Ok());
        }, "Rename failed");
    }

    [RelayCommand]
    private async Task SetClientPasswordAsync()
    {
        await RunBusyAsync(async () =>
        {
            var response = await _client.SetClientPasswordAsync(NewClientPassword);
            SetStatus(response.Ok() ? "Client password updated." : $"Failed: {response.ErrorText()}", !response.Ok());
            if (response.Ok()) NewClientPassword = string.Empty;
        }, "Set client password failed");
    }

    [RelayCommand]
    private async Task SetAdminPasswordAsync()
    {
        await RunBusyAsync(async () =>
        {
            var response = await _client.SetAdminPasswordAsync(NewAdminPassword);
            SetStatus(response.Ok() ? "Admin password updated." : $"Failed: {response.ErrorText()}", !response.Ok());
            if (response.Ok()) NewAdminPassword = string.Empty;
        }, "Set admin password failed");
    }

    [RelayCommand]
    private async Task ShutdownAsync()
    {
        if (!await _dialogs.ConfirmAsync("Shutdown server", "Shut down the dedicated server now?"))
            return;

        await RunBusyAsync(async () =>
        {
            var response = await _client.ShutdownAsync();
            SetStatus(response.Ok() ? "Shutdown requested." : $"Failed: {response.ErrorText()}", !response.Ok());
        }, "Shutdown failed");
    }

    private static void Fill(ObservableCollection<KeyValueRow> target, IDictionary<string, string>? source)
    {
        target.Clear();
        if (source is null) return;
        foreach (var kvp in source)
            target.Add(new KeyValueRow { Key = kvp.Key, Value = kvp.Value });
    }

    private static Dictionary<string, string> ToDictionary(ObservableCollection<KeyValueRow> rows)
    {
        var dict = new Dictionary<string, string>();
        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Key))
                dict[row.Key] = row.Value;
        }
        return dict;
    }
}
