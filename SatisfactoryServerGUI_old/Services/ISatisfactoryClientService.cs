using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SatisfactorySdk;
using SatisfactorySdk.DTO;
using SatisfactoryServerGUI.Models;

namespace SatisfactoryServerGUI.Services;

public interface ISatisfactoryClientService
{
    bool IsConfigured { get; }
    ConnectionStatus Status { get; }
    string? CurrentToken { get; }

    event EventHandler<ConnectionStatus>? StatusChanged;

    void Configure(ServerProfile profile);
    void SetToken(string token);
    void Disconnect();

    Task<ClientResponse<HealthCheckResponse>> HealthCheckAsync(string clientCustomData = "");
    Task<ClientResponse<bool>> VerifyAuthenticationTokenAsync();
    Task<ClientResponse<AuthTokenResponse>> PasswordlessLoginAsync(PrivilegeLevelEnum level);
    Task<ClientResponse<AuthTokenResponse>> PasswordLoginAsync(string password, PrivilegeLevelEnum level);
    Task<ClientResponse<AuthTokenResponse>> ClaimServerAsync(string serverName, string adminPassword);

    Task<ClientResponse<bool>> RenameServerAsync(string name);
    Task<ClientResponse<bool>> SetClientPasswordAsync(string password);
    Task<ClientResponse<AuthTokenResponse>> SetAdminPasswordAsync(string password);

    Task<ClientResponse<QueryServerStateResponse>> QueryServerStateAsync();
    Task<ServerStateResponse?> PollServerStateUdpAsync();

    Task<ClientResponse<GetServerOptionsResponse>> GetServerOptionsAsync();
    Task<ClientResponse<bool>> ApplyServerOptionsAsync(Dictionary<string, string> options);
    Task<ClientResponse<GetAdvancedGameSettingsResponse>> GetAdvancedGameSettingsAsync();
    Task<ClientResponse<bool>> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> settings);

    Task<ClientResponse<RunCommandResponse>> RunCommandAsync(string command);

    Task<ClientResponse<EnumerateSessionsResponse>> EnumerateSessionsAsync();
    Task<ClientResponse<bool>> SaveGameAsync(string saveName);
    Task<ClientResponse<bool>> DeleteSaveFileAsync(string saveName);
    Task<ClientResponse<bool>> DeleteSaveSessionAsync(string sessionName);
    Task<ClientResponse<bool>> SetAutoLoadSessionNameAsync(string sessionName);
    Task<ClientResponse<bool>> DownloadSaveGameAsync(string saveName, string destPath);
    Task<ClientResponse<bool>> UploadSaveGameAsync(string saveName, string srcPath, bool loadOnUpload, bool enableAdvancedGameSettings);

    Task<ClientResponse<bool>> ShutdownAsync();
}
