using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SatisfactoryServerGUI.ViewModels.Pages;

namespace SatisfactoryServerGUI.Views.Pages;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
