using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SatisfactoryServerGUI.ViewModels.Pages;

namespace SatisfactoryServerGUI.Views.Pages;

public partial class AuthenticationView : UserControl
{
    public AuthenticationView()
    {
        InitializeComponent();
        DataContext = new AuthenticationViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
