using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Models;
using SatisfactoryServerGUI.Services;

namespace SatisfactoryServerGUI.ViewModels;

public partial class ProfilePaneViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly ISatisfactoryClientService _client;
    private readonly IConnectionService _connection;
    private readonly IDialogService _dialogs;

    public ObservableCollection<ServerProfile> Profiles { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private ServerProfile? _selectedProfile;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private ServerProfile? _editingProfile;

    [ObservableProperty]
    private bool _isConnected;

    public bool HasSelection => SelectedProfile != null;

    public ProfilePaneViewModel(
        ISettingsService settings,
        ISatisfactoryClientService client,
        IConnectionService connection,
        IDialogService dialogs)
    {
        _settings = settings;
        _client = client;
        _connection = connection;
        _dialogs = dialogs;

        _client.StatusChanged += (_, status) => IsConnected = status == ConnectionStatus.Connected;
    }

    public void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var p in _settings.Profiles)
            Profiles.Add(p);
        SelectedProfile = _settings.ActiveProfile;
    }

    partial void OnSelectedProfileChanged(ServerProfile? value)
    {
        _settings.SetActiveProfile(value?.Id);
    }

    [RelayCommand]
    private void AddProfile()
    {
        EditingProfile = new ServerProfile();
        IsEditing = true;
    }

    [RelayCommand]
    private void EditProfile()
    {
        if (SelectedProfile is null) return;
        EditingProfile = SelectedProfile.Clone();
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (EditingProfile is null) return;

        var existing = Profiles.FirstOrDefault(p => p.Id == EditingProfile.Id);
        if (existing is null)
        {
            _settings.AddProfile(EditingProfile);
            Profiles.Add(EditingProfile);
        }
        else
        {
            _settings.UpdateProfile(EditingProfile);
            var index = Profiles.IndexOf(existing);
            Profiles[index] = EditingProfile;
        }

        SelectedProfile = EditingProfile;
        IsEditing = false;
        EditingProfile = null;
        await _settings.SaveAsync();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingProfile = null;
    }

    [RelayCommand]
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile is null) return;
        if (!await _dialogs.ConfirmAsync("Delete profile", $"Delete '{SelectedProfile.Name}'?"))
            return;

        var toRemove = SelectedProfile;
        _settings.RemoveProfile(toRemove.Id);
        Profiles.Remove(toRemove);
        SelectedProfile = _settings.ActiveProfile;
        await _settings.SaveAsync();
    }

    [RelayCommand]
    private void Connect()
    {
        if (SelectedProfile is null)
        {
            SetStatus("Select a profile first.", isError: true);
            return;
        }

        _client.Configure(SelectedProfile);
        _connection.Start(
            TimeSpan.FromSeconds(Math.Max(2, _settings.Settings.HttpsPollSeconds)),
            TimeSpan.FromSeconds(Math.Max(1, _settings.Settings.UdpPollSeconds)));
        SetStatus($"Connecting to {SelectedProfile.Host}:{SelectedProfile.HttpsPort}...");
    }

    [RelayCommand]
    private void Disconnect()
    {
        _connection.Stop();
        _client.Disconnect();
        IsConnected = false;
        SetStatus("Disconnected.");
    }
}
