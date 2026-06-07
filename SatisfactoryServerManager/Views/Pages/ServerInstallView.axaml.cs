using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SatisfactoryServerManager.Views.Pages;

public partial class ServerInstallView : UserControl
{
    public ServerInstallView()
    {
        InitializeComponent();
        AddBuildContent.CloseRequested += (_, _) => SetDrawerOpen(false);
    }

    private void OpenAddBuild_Click(object? sender, RoutedEventArgs e)
    {
        SetDrawerOpen(true);
    }

    private void CloseAddBuild_Click(object? sender, RoutedEventArgs e)
    {
        SetDrawerOpen(false);
    }

    private void SetDrawerOpen(bool isOpen)
    {
        DrawerScrim.Classes.Set("open", isOpen);
        AddBuildDrawer.Classes.Set("open", isOpen);

        if (isOpen)
        {
            AddBuildContent.FocusFirstField();
        }
    }
}
