using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SatisfactoryServerGUI.Models;

namespace SatisfactoryServerGUI.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    IReadOnlyList<ServerProfile> Profiles { get; }
    ServerProfile? ActiveProfile { get; }

    event EventHandler<ServerProfile?>? ActiveProfileChanged;

    Task LoadAsync();
    Task SaveAsync();

    void AddProfile(ServerProfile profile);
    void UpdateProfile(ServerProfile profile);
    void RemoveProfile(Guid id);
    void SetActiveProfile(Guid? id);
}
