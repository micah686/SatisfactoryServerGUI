using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Models;
using SatisfactoryServerGUI.Services;

namespace SatisfactoryServerGUI.ViewModels;

public partial class SavesViewModel : ViewModelBase
{
    private readonly ISatisfactoryClientService _client;
    private readonly IDialogService _dialogs;

    public ObservableCollection<SaveItemViewData> Saves { get; } = new();

    [ObservableProperty]
    private SaveItemViewData? _selectedSave;

    public SavesViewModel(ISatisfactoryClientService client, IDialogService dialogs)
    {
        _client = client;
        _dialogs = dialogs;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            var response = await _client.EnumerateSessionsAsync();
            if (!response.Ok())
            {
                SetStatus($"Enumerate failed: {response.ErrorText()}", isError: true);
                return;
            }

            Saves.Clear();
            var data = response.RequestResponse?.Data;
            if (data?.Sessions is { } sessions)
            {
                foreach (var session in sessions)
                {
                    if (session.SaveHeaders is null) continue;
                    foreach (var header in session.SaveHeaders)
                        Saves.Add(SaveItemViewData.FromHeader(header));
                }
            }

            SetStatus($"Loaded {Saves.Count} save file(s).");
        }, "Enumerate failed");
    }

    [RelayCommand]
    private async Task SaveGameAsync()
    {
        var name = await _dialogs.PromptAsync("Save game", "New save file name:", "MySave");
        if (string.IsNullOrWhiteSpace(name)) return;

        await RunBusyAsync(async () =>
        {
            var response = await _client.SaveGameAsync(name);
            SetStatus(response.Ok() ? $"Saved '{name}'." : $"Save failed: {response.ErrorText()}", !response.Ok());
            if (response.Ok()) await RefreshAsync();
        }, "Save failed");
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (SelectedSave is null) return;

        var confirm = await _dialogs.ConfirmAsync(
            "Load save",
            $"Set '{SelectedSave.SessionName}' as the auto-load session?\n\n" +
            "The SDK has no direct load call, so the server must be restarted to load it " +
            "(use the Local Server tab, or restart the server externally).");
        if (!confirm) return;

        await RunBusyAsync(async () =>
        {
            var response = await _client.SetAutoLoadSessionNameAsync(SelectedSave.SessionName);
            SetStatus(response.Ok()
                ? $"Auto-load set to '{SelectedSave.SessionName}'. Restart the server to load it."
                : $"Failed: {response.ErrorText()}", !response.Ok());
        }, "Load failed");
    }

    [RelayCommand]
    private async Task SetAutoLoadAsync()
    {
        if (SelectedSave is null) return;
        await RunBusyAsync(async () =>
        {
            var response = await _client.SetAutoLoadSessionNameAsync(SelectedSave.SessionName);
            SetStatus(response.Ok()
                ? $"Auto-load session set to '{SelectedSave.SessionName}'."
                : $"Failed: {response.ErrorText()}", !response.Ok());
        }, "Set auto-load failed");
    }

    [RelayCommand]
    private async Task DeleteSaveAsync()
    {
        if (SelectedSave is null) return;
        if (!await _dialogs.ConfirmAsync("Delete save", $"Delete save file '{SelectedSave.SaveName}'?"))
            return;

        await RunBusyAsync(async () =>
        {
            var response = await _client.DeleteSaveFileAsync(SelectedSave.SaveName);
            SetStatus(response.Ok() ? "Save deleted." : $"Delete failed: {response.ErrorText()}", !response.Ok());
            if (response.Ok()) await RefreshAsync();
        }, "Delete failed");
    }

    [RelayCommand]
    private async Task DeleteSessionAsync()
    {
        if (SelectedSave is null) return;
        if (!await _dialogs.ConfirmAsync("Delete session",
            $"Delete ALL saves for session '{SelectedSave.SessionName}'?"))
            return;

        await RunBusyAsync(async () =>
        {
            var response = await _client.DeleteSaveSessionAsync(SelectedSave.SessionName);
            SetStatus(response.Ok() ? "Session deleted." : $"Delete failed: {response.ErrorText()}", !response.Ok());
            if (response.Ok()) await RefreshAsync();
        }, "Delete session failed");
    }

    [RelayCommand]
    private async Task UploadAsync()
    {
        var path = await _dialogs.PickFileAsync("Select a .sav file to upload");
        if (string.IsNullOrWhiteSpace(path)) return;

        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        await RunBusyAsync(async () =>
        {
            var response = await _client.UploadSaveGameAsync(name, path, loadOnUpload: false, enableAdvancedGameSettings: false);
            SetStatus(response.Ok() ? $"Uploaded '{name}'." : $"Upload failed: {response.ErrorText()}", !response.Ok());
            if (response.Ok()) await RefreshAsync();
        }, "Upload failed");
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (SelectedSave is null) return;
        var path = await _dialogs.SaveFileAsync("Download save as", SelectedSave.SaveName + ".sav");
        if (string.IsNullOrWhiteSpace(path)) return;

        await RunBusyAsync(async () =>
        {
            var response = await _client.DownloadSaveGameAsync(SelectedSave.SaveName, path);
            SetStatus(response.Ok() ? $"Downloaded to '{path}'." : $"Download failed: {response.ErrorText()}", !response.Ok());
        }, "Download failed");
    }
}
