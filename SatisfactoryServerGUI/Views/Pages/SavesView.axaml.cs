using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SatisfactoryServerGUI.ViewModels.Pages;

namespace SatisfactoryServerGUI.Views.Pages;

public partial class SavesView : UserControl
{
    public SavesView()
    {
        InitializeComponent();
        DataContext = new SavesViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
