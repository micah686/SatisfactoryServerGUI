using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SatisfactoryServerGUI.ViewModels.Pages;

namespace SatisfactoryServerGUI.Views.Pages;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
