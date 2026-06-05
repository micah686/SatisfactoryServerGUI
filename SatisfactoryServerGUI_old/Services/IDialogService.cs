using System.Threading.Tasks;

namespace SatisfactoryServerGUI.Services;

public interface IDialogService
{
    Task<string?> PickFileAsync(string title);
    Task<string?> PickFolderAsync(string title);
    Task<string?> SaveFileAsync(string title, string suggestedName);

    Task<bool> ConfirmAsync(string title, string message);
    Task<string?> PromptAsync(string title, string message, string defaultValue = "");
}
