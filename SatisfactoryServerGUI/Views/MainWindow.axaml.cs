using Avalonia.Markup.Xaml;
using PleasantUI.Controls;

namespace SatisfactoryServerGUI.Views;

public partial class MainWindow : PleasantWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
