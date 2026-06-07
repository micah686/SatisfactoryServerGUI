using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace SatisfactoryServerManager.Views.UserControls;

public partial class AddBuildView : UserControl
{
    public AddBuildView()
    {
        InitializeComponent();
    }

    public event EventHandler? CloseRequested;

    public void FocusFirstField()
    {
        Dispatcher.UIThread.Post(() => LabelTextBox.Focus(), DispatcherPriority.Background);
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Install_Click(object? sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
