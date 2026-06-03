using SatisfactorySdk;
using SatisfactorySdk.DTO;
using System.Threading.Tasks;

namespace SatisfactoryServerGUI.Services;

public interface ISatisfactoryClientService
{
    bool IsConfigured { get; }
    SatisfactoryClient? Client { get; }
    void Configure(string ip, int port, string? authToken, bool trustSelfSignedCerts);
    Task<ClientResponse<HealthCheckResponse>> HealthCheckAsync(string clientCustomData = "");
    Task<ClientResponse<AuthTokenResponse>> PasswordlessLoginAsync(PrivilegeLevelEnum privilegeLevel);
    Task<ClientResponse<AuthTokenResponse>> PasswordLoginAsync(string password, PrivilegeLevelEnum privilegeLevel);
    Task<ClientResponse<AuthTokenResponse>> ClaimServerAsync(string serverName, string adminPassword);
    Task<ClientResponse<QueryServerStateResponse>> QueryServerStateAsync();
    Task<ClientResponse<GetServerOptionsResponse>> GetServerOptionsAsync();
    Task<ClientResponse<GetAdvancedGameSettingsResponse>> GetAdvancedGameSettingsAsync();
    Task<ClientResponse<bool>> ApplyServerOptionsAsync(System.Collections.Generic.Dictionary<string, string> settings);
    Task<ClientResponse<bool>> RenameServerAsync(string serverName);
    Task<ClientResponse<bool>> ShutdownAsync();
    Task<ClientResponse<bool>> SaveGameAsync(string saveName);
    Task<ClientResponse<EnumerateSessionsResponse>> EnumerateSessionsAsync();
    Task<ClientResponse<bool>> RunCommandAsync(string command);
    Task<bool> VerifyAuthenticationTokenAsync();
}
