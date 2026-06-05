using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatisfactorySdk;
using SatisfactoryServerGUI.Services;

namespace SatisfactoryServerGUI.ViewModels;

public partial class AuthViewModel : ViewModelBase
{
    private readonly ISatisfactoryClientService _client;
    private readonly ISettingsService _settings;

    [ObservableProperty]
    private string _tokenInput = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _loginAsAdmin = true;

    [ObservableProperty]
    private string _claimServerName = "My Satisfactory Server";

    [ObservableProperty]
    private string _claimAdminPassword = string.Empty;

    public AuthViewModel(ISatisfactoryClientService client, ISettingsService settings)
    {
        _client = client;
        _settings = settings;
    }

    [RelayCommand]
    private async Task ApplyTokenAsync()
    {
        if (!_client.IsConfigured)
        {
            SetStatus("Connect to a profile first.", isError: true);
            return;
        }

        await RunBusyAsync(async () =>
        {
            _client.SetToken(TokenInput.Trim());
            var verify = await _client.VerifyAuthenticationTokenAsync();
            if (verify.Ok())
            {
                await PersistTokenAsync(TokenInput.Trim());
                SetStatus("Token applied and verified.");
            }
            else
            {
                SetStatus($"Token rejected: {verify.ErrorText()}", isError: true);
            }
        }, "Apply token failed");
    }

    [RelayCommand]
    private async Task PasswordLoginAsync()
    {
        if (!_client.IsConfigured)
        {
            SetStatus("Connect to a profile first.", isError: true);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var level = LoginAsAdmin ? PrivilegeLevelEnum.Administrator : PrivilegeLevelEnum.Client;
            var response = await _client.PasswordLoginAsync(Password, level);
            if (response.Ok() && response.RequestResponse?.Data?.AuthenticationToken is { Length: > 0 } token)
            {
                _client.SetToken(token);
                await PersistTokenAsync(token);
                SetStatus($"Logged in as {level}.");
                Password = string.Empty;
            }
            else
            {
                SetStatus($"Login failed: {response.ErrorText()}", isError: true);
            }
        }, "Login failed");
    }

    [RelayCommand]
    private async Task ClaimServerAsync()
    {
        if (!_client.IsConfigured)
        {
            SetStatus("Connect to a profile first.", isError: true);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var response = await _client.ClaimServerAsync(ClaimServerName, ClaimAdminPassword);
            if (response.Ok() && response.RequestResponse?.Data?.AuthenticationToken is { Length: > 0 } token)
            {
                _client.SetToken(token);
                await PersistTokenAsync(token);
                SetStatus("Server claimed. Admin token saved.");
                ClaimAdminPassword = string.Empty;
            }
            else
            {
                SetStatus($"Claim failed: {response.ErrorText()}", isError: true);
            }
        }, "Claim failed");
    }

    private async Task PersistTokenAsync(string token)
    {
        if (_settings.ActiveProfile is { } profile)
        {
            profile.AuthToken = token;
            _settings.UpdateProfile(profile);
            await _settings.SaveAsync();
        }
    }
}
