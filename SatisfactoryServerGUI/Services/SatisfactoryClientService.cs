using SatisfactorySdk;
using SatisfactorySdk.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SatisfactoryServerGUI.Services;

public class SatisfactoryClientService : ISatisfactoryClientService
{
    private SatisfactoryClient? _client;

    public bool IsConfigured => _client != null;
    public SatisfactoryClient? Client => _client;

    public void Configure(string ip, int port, string? authToken, bool trustSelfSignedCerts)
    {
        _client = new SatisfactoryClient(ip, port, authToken: authToken ?? "", trustSelfSignedCerts: trustSelfSignedCerts);
    }

    public async Task<ClientResponse<HealthCheckResponse>> HealthCheckAsync(string clientCustomData = "")
    {
        if (_client == null) return NotConfiguredResponse<HealthCheckResponse>();
        return await _client.HealthCheckAsync(clientCustomData);
    }

    public async Task<ClientResponse<AuthTokenResponse>> PasswordlessLoginAsync(PrivilegeLevelEnum privilegeLevel)
    {
        if (_client == null) return NotConfiguredResponse<AuthTokenResponse>();
        return await _client.PasswordlessLoginAsync(privilegeLevel);
    }

    public async Task<ClientResponse<AuthTokenResponse>> PasswordLoginAsync(string password, PrivilegeLevelEnum privilegeLevel)
    {
        if (_client == null) return NotConfiguredResponse<AuthTokenResponse>();
        return await _client.PasswordLoginAsync(password, privilegeLevel);
    }

    public async Task<ClientResponse<AuthTokenResponse>> ClaimServerAsync(string serverName, string adminPassword)
    {
        if (_client == null) return NotConfiguredResponse<AuthTokenResponse>();
        return await _client.ClaimServerAsync(serverName, adminPassword);
    }

    public async Task<ClientResponse<QueryServerStateResponse>> QueryServerStateAsync()
    {
        if (_client == null) return NotConfiguredResponse<QueryServerStateResponse>();
        return await _client.QueryServerStateAsync();
    }

    public async Task<ClientResponse<GetServerOptionsResponse>> GetServerOptionsAsync()
    {
        if (_client == null) return NotConfiguredResponse<GetServerOptionsResponse>();
        return await _client.GetServerOptionsAsync();
    }

    public async Task<ClientResponse<GetAdvancedGameSettingsResponse>> GetAdvancedGameSettingsAsync()
    {
        if (_client == null) return NotConfiguredResponse<GetAdvancedGameSettingsResponse>();
        return await _client.GetAdvancedGameSettingsAsync();
    }

    public async Task<ClientResponse<bool>> ApplyServerOptionsAsync(Dictionary<string, string> settings)
    {
        if (_client == null) return NotConfiguredResponse<bool>();
        return await _client.ApplyServerOptionsAsync(settings);
    }

    public async Task<ClientResponse<bool>> RenameServerAsync(string serverName)
    {
        if (_client == null) return NotConfiguredResponse<bool>();
        return await _client.RenameServerAsync(serverName);
    }

    public async Task<ClientResponse<bool>> ShutdownAsync()
    {
        if (_client == null) return NotConfiguredResponse<bool>();
        return await _client.ShutdownAsync();
    }

    public async Task<ClientResponse<bool>> SaveGameAsync(string saveName)
    {
        if (_client == null) return NotConfiguredResponse<bool>();
        return await _client.SaveGameAsync(saveName);
    }

    public async Task<ClientResponse<EnumerateSessionsResponse>> EnumerateSessionsAsync()
    {
        if (_client == null) return NotConfiguredResponse<EnumerateSessionsResponse>();
        return await _client.EnumerateSessionsAsync();
    }

    public async Task<ClientResponse<bool>> RunCommandAsync(string command)
    {
        if (_client == null) return NotConfiguredResponse<bool>();
        var result = await _client.RunCommandAsync(command);
        return new ClientResponse<bool>
        {
            IsSuccessful = result.IsSuccessful,
            StatusCode = result.StatusCode,
            ErrorResponse = result.ErrorResponse,
            RequestResponse = result.RequestResponse != null ? new RequestResponse<bool> { Data = true } : null
        };
    }

    public async Task<bool> VerifyAuthenticationTokenAsync()
    {
        if (_client == null) return false;
        var result = await _client.VerifyAuthenticationTokenAsync();
        return result.IsSuccessful;
    }

    private static ClientResponse<T> NotConfiguredResponse<T>()
    {
        return new ClientResponse<T>
        {
            IsSuccessful = false,
            ErrorResponse = new ErrorResponse
            {
                ErrorCode = "not_configured",
                ErrorMessage = "API client is not configured. Please check connection settings."
            }
        };
    }
}
