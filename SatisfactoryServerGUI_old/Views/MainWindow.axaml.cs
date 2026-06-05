using Avalonia.Controls;
using Avalonia.Interactivity;
using SatisfactoryServerGUI.ViewModels;

namespace SatisfactoryServerGUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is MainWindowViewModel vm)
            await vm.InitializeAsync();
    }
}
