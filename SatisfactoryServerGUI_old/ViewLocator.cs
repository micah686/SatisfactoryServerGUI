using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using SatisfactoryServerGUI.ViewModels;

namespace SatisfactoryServerGUI;

/// <summary>
/// Resolves a *ViewModel instance to its matching *View by naming convention,
/// e.g. DashboardViewModel -> SatisfactoryServerGUI_old.Views.DashboardView.
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var vmName = param.GetType().FullName!;
        var viewName = vmName
            .Replace(".ViewModels.", ".Views.")
            .Replace("ViewModel", "View");

        var type = Type.GetType(viewName);
        if (type != null)
            return (Control)Activator.CreateInstance(type)!;

        return new TextBlock { Text = "Not Found: " + viewName };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
