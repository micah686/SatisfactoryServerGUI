using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SatisfactoryServerManager.ViewModels.Pages;

public partial class ServerProfilesViewModel: ViewModelBase
{
    [ObservableProperty]
    private bool _isDrawerOpen;

    [RelayCommand]
    private void OpenDrawer()
    {
        IsDrawerOpen = true;
    }

    [RelayCommand]
    private void CloseDrawer()
    {
        IsDrawerOpen = false;
    }
}