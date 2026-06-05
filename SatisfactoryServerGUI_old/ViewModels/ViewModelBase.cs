using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SatisfactoryServerGUI.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>True when the status message represents an error (drives colouring).</summary>
    [ObservableProperty]
    private bool _statusIsError;

    protected void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }

    /// <summary>Runs work with the busy flag set and surfaces exceptions to the status line.</summary>
    protected async Task RunBusyAsync(Func<Task> work, string? failurePrefix = null)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            SetStatus($"{failurePrefix}{(failurePrefix is null ? "" : ": ")}{ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
