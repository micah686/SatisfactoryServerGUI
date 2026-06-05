using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SatisfactoryServerGUI.Models;

namespace SatisfactoryServerGUI.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _configPath;

    public AppSettings Settings { get; private set; } = new();

    public IReadOnlyList<ServerProfile> Profiles => Settings.Profiles;

    public ServerProfile? ActiveProfile { get; private set; }

    public event EventHandler<ServerProfile?>? ActiveProfileChanged;

    public SettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SatisfactoryServerGUI_old");
        Directory.CreateDirectory(dir);
        _configPath = Path.Combine(dir, "settings.json");
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = await File.ReadAllTextAsync(_configPath).ConfigureAwait(false);
                Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            // Corrupt/unreadable config — fall back to defaults rather than crashing.
            Settings = new AppSettings();
        }

        ActiveProfile = Settings.LastActiveProfileId is { } id
            ? Settings.Profiles.FirstOrDefault(p => p.Id == id)
            : Settings.Profiles.FirstOrDefault();

        ActiveProfileChanged?.Invoke(this, ActiveProfile);
    }

    public async Task SaveAsync()
    {
        Settings.LastActiveProfileId = ActiveProfile?.Id;

        var json = JsonSerializer.Serialize(Settings, JsonOptions);

        // Atomic write: temp file then move, so an interrupted write can't corrupt the config.
        var tmp = _configPath + ".tmp";
        await File.WriteAllTextAsync(tmp, json).ConfigureAwait(false);
        File.Move(tmp, _configPath, overwrite: true);
    }

    public void AddProfile(ServerProfile profile)
    {
        Settings.Profiles.Add(profile);
    }

    public void UpdateProfile(ServerProfile profile)
    {
        var index = Settings.Profiles.FindIndex(p => p.Id == profile.Id);
        if (index >= 0)
            Settings.Profiles[index] = profile;
        else
            Settings.Profiles.Add(profile);

        if (ActiveProfile?.Id == profile.Id)
        {
            ActiveProfile = profile;
            ActiveProfileChanged?.Invoke(this, ActiveProfile);
        }
    }

    public void RemoveProfile(Guid id)
    {
        var profile = Settings.Profiles.FirstOrDefault(p => p.Id == id);
        if (profile == null)
            return;

        Settings.Profiles.Remove(profile);

        if (ActiveProfile?.Id == id)
            SetActiveProfile(Settings.Profiles.FirstOrDefault()?.Id);
    }

    public void SetActiveProfile(Guid? id)
    {
        var next = id is { } realId
            ? Settings.Profiles.FirstOrDefault(p => p.Id == realId)
            : null;

        if (ReferenceEquals(next, ActiveProfile))
            return;

        ActiveProfile = next;
        Settings.LastActiveProfileId = next?.Id;
        ActiveProfileChanged?.Invoke(this, ActiveProfile);
    }
}
