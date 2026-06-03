using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactoryServerGUI.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SatisfactoryServerGUI.ViewModels;

public partial class ServerFoldersViewModel : ViewModelBase
{
    private ISettingsService _settings = null!;

    [ObservableProperty]
    private string _serverPath = "";

    [ObservableProperty]
    private string _statusMessage = "";

    public void Initialize(ISettingsService settings)
    {
        _settings = settings;
        ServerPath = settings.ServerPath;

        settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ISettingsService.ServerPath))
                ServerPath = settings.ServerPath;
        };
    }

    [RelayCommand]
    private void OpenRootFolder()
    {
        if (ValidateAndOpen(_settings.ServerPath, "Root")) return;
    }

    [RelayCommand]
    private void OpenServerRoot()
    {
        var path = Path.Combine(_settings.ServerPath, "satisfactorydedicatedserver");
        ValidateAndOpen(path, "Server Root");
    }

    [RelayCommand]
    private void OpenServerSaves()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        string savesPath;

        if (isWindows)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            savesPath = Path.Combine(localAppData, "FactoryGame", "Saved", "SaveGames", "server");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            savesPath = Path.Combine(home, ".config", "Epic", "FactoryGame", "Saved", "SaveGames", "server");
        }

        ValidateAndOpen(savesPath, "Server Saves");
    }

    [RelayCommand]
    private void OpenServerLogs()
    {
        var logsPath = Path.Combine(_settings.ServerPath, "satisfactorydedicatedserver", "FactoryGame", "Saved", "Logs");
        ValidateAndOpen(logsPath, "Server Logs");
    }

    private bool ValidateAndOpen(string path, string folderName)
    {
        if (!Directory.Exists(path))
        {
            StatusMessage = $"{folderName} folder not found: {path}";
            return false;
        }

        try
        {
            OpenFolder(path);
            StatusMessage = $"Opened {folderName} folder";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open {folderName} folder: {ex.Message}";
            return false;
        }
    }

    private static void OpenFolder(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = false
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Try common Linux file managers
            var managers = new[] { "xdg-open", "nautilus", "dolphin", "pcmanfm", "thunar" };
            foreach (var manager in managers)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = manager,
                        Arguments = $"\"{path}\"",
                        UseShellExecute = false
                    });
                    return;
                }
                catch { }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"\"{path}\"",
                UseShellExecute = false
            });
        }
    }
}
