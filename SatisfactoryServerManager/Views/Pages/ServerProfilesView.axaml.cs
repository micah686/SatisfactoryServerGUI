using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SatisfactoryServerManager.ViewModels.Pages;

namespace SatisfactoryServerManager.Views.Pages;

public partial class ServerProfilesView : UserControl
{
    public ServerProfilesView()
    {
        InitializeComponent();
        DataContext = new ServerProfilesViewModel();
    }
}