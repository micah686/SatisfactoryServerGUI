using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using SatisfactorySdk;
using SatisfactorySdk.DTO;
using SatisfactoryServerGUI.Models;

namespace SatisfactoryServerGUI.Services;

public class SatisfactoryClientService : ISatisfactoryClientService
{
    private SatisfactoryClient? _client;
    private ServerProfile? _profile;
    private ConnectionStatus _status = ConnectionStatus.Disconnected;

    public bool IsConfigured => _client != null;
    public string? CurrentToken { get; private set; }

    public ConnectionStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value) return;
            _status = value;
            StatusChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<ConnectionStatus>? StatusChanged;

    public void Configure(ServerProfile profile)
    {
        _profile = profile;
        CurrentToken = profile.AuthToken;
        // Pass null HttpClient so the SDK builds its own handler honouring trustSelfSignedCerts.
        _client = new SatisfactoryClient(
            profile.Host,
            profile.HttpsPort,
            profile.QueryPort,
            profile.AuthToken ?? string.Empty,
            profile.TrustSelfSignedCerts);
        Status = ConnectionStatus.Disconnected;
    }

    public void SetToken(string token)
    {
        CurrentToken = token;
        if (_profile != null)
            _profile.AuthToken = token;

        if (_client != null && !_client.TrySetAuthToken(token) && _profile != null)
        {
            // Fall back to rebuilding the client if the token couldn't be applied in place.
            Configure(_profile);
        }
    }

    public void Disconnect()
    {
        _client = null;
        _profile = null;
        CurrentToken = null;
        Status = ConnectionStatus.Disconnected;
    }

    public Task<ClientResponse<HealthCheckResponse>> HealthCheckAsync(string clientCustomData = "")
        => Invoke(c => c.HealthCheckAsync(clientCustomData));

    public Task<ClientResponse<bool>> VerifyAuthenticationTokenAsync()
        => Invoke(c => c.VerifyAuthenticationTokenAsync());

    public Task<ClientResponse<AuthTokenResponse>> PasswordlessLoginAsync(PrivilegeLevelEnum level)
        => Invoke(c => c.PasswordlessLoginAsync(level));

    public Task<ClientResponse<AuthTokenResponse>> PasswordLoginAsync(string password, PrivilegeLevelEnum level)
        => Invoke(c => c.PasswordLoginAsync(password, level));

    public Task<ClientResponse<AuthTokenResponse>> ClaimServerAsync(string serverName, string adminPassword)
        => Invoke(c => c.ClaimServerAsync(serverName, adminPassword));

    public Task<ClientResponse<bool>> RenameServerAsync(string name)
        => Invoke(c => c.RenameServerAsync(name));

    public Task<ClientResponse<bool>> SetClientPasswordAsync(string password)
        => Invoke(c => c.SetClientPasswordAsync(password));

    public Task<ClientResponse<AuthTokenResponse>> SetAdminPasswordAsync(string password)
        => Invoke(c => c.SetAdminPasswordAsync(password));

    public Task<ClientResponse<QueryServerStateResponse>> QueryServerStateAsync()
        => Invoke(c => c.QueryServerStateAsync());

    public async Task<ServerStateResponse?> PollServerStateUdpAsync()
    {
        if (_client == null) return null;
        try
        {
            return await _client.PollServerStateUdpAsync().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public Task<ClientResponse<GetServerOptionsResponse>> GetServerOptionsAsync()
        => Invoke(c => c.GetServerOptionsAsync());

    public Task<ClientResponse<bool>> ApplyServerOptionsAsync(Dictionary<string, string> options)
        => Invoke(c => c.ApplyServerOptionsAsync(options));

    public Task<ClientResponse<GetAdvancedGameSettingsResponse>> GetAdvancedGameSettingsAsync()
        => Invoke(c => c.GetAdvancedGameSettingsAsync());

    public Task<ClientResponse<bool>> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> settings)
        => Invoke(c => c.ApplyAdvancedGameSettingsAsync(settings));

    public Task<ClientResponse<RunCommandResponse>> RunCommandAsync(string command)
        => Invoke(c => c.RunCommandAsync(command));

    public Task<ClientResponse<EnumerateSessionsResponse>> EnumerateSessionsAsync()
        => Invoke(c => c.EnumerateSessionsAsync());

    public Task<ClientResponse<bool>> SaveGameAsync(string saveName)
        => Invoke(c => c.SaveGameAsync(saveName));

    public Task<ClientResponse<bool>> DeleteSaveFileAsync(string saveName)
        => Invoke(c => c.DeleteSaveFileAsync(saveName));

    public Task<ClientResponse<bool>> DeleteSaveSessionAsync(string sessionName)
        => Invoke(c => c.DeleteSaveSessionAsync(sessionName));

    public Task<ClientResponse<bool>> SetAutoLoadSessionNameAsync(string sessionName)
        => Invoke(c => c.SetAutoLoadSessionName(sessionName));

    public Task<ClientResponse<bool>> DownloadSaveGameAsync(string saveName, string destPath)
        => Invoke(c => c.DownloadSaveGameAsync(saveName, destPath));

    public Task<ClientResponse<bool>> UploadSaveGameAsync(string saveName, string srcPath, bool loadOnUpload, bool enableAdvancedGameSettings)
        => Invoke(c => c.UploadSaveGameAsync(saveName, srcPath, loadOnUpload, enableAdvancedGameSettings));

    public Task<ClientResponse<bool>> ShutdownAsync()
        => Invoke(c => c.ShutdownAsync());

    /// <summary>
    /// Runs an SDK call, translating transport exceptions into a failed ClientResponse
    /// and keeping the connection status in sync.
    /// </summary>
    private async Task<ClientResponse<T>> Invoke<T>(Func<SatisfactoryClient, Task<ClientResponse<T>>> call)
    {
        if (_client == null)
            return Failed<T>("No server configured.");

        try
        {
            var response = await call(_client).ConfigureAwait(false);
            if (response.IsSuccessful)
                Status = ConnectionStatus.Connected;
            else if (response.HasError)
                Status = ConnectionStatus.Connected; // reachable, but the request was rejected
            return response;
        }
        catch (Exception ex)
        {
            Status = ConnectionStatus.Error;
            return Failed<T>(ex.Message);
        }
    }

    private static ClientResponse<T> Failed<T>(string message) => new()
    {
        IsSuccessful = false,
        StatusCode = HttpStatusCode.ServiceUnavailable,
        ErrorResponse = new ErrorResponse { ErrorCode = "transport_error", ErrorMessage = message }
    };
}
