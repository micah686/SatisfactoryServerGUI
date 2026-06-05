using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SatisfactoryServerGUI.ViewModels.Pages;

namespace SatisfactoryServerGUI.Views.Pages;

public partial class ConsoleView : UserControl
{
    public ConsoleView()
    {
        InitializeComponent();
        DataContext = new ConsoleViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
