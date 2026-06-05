using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SatisfactoryServerGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _selectedProfile;

    public ObservableCollection<string> ServerProfiles { get; } = new()
    {
        "Default Server",
        "Test Server",
    };

    public MainWindowViewModel()
    {
        _selectedProfile = ServerProfiles[0];
    }
}
