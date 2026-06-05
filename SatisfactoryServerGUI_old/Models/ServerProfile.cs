using System;

namespace SatisfactoryServerGUI.Models;

/// <summary>
/// A saved server connection. Persisted to settings.json.
/// </summary>
public class ServerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Server";
    public string Host { get; set; } = "127.0.0.1";
    public int HttpsPort { get; set; } = 7777;
    public int QueryPort { get; set; } = 7777;

    /// <summary>Bearer auth token (from server.GenerateAPIToken or a login/claim flow).</summary>
    public string? AuthToken { get; set; }

    /// <summary>Dedicated servers use self-signed certs; default to trusting them.</summary>
    public bool TrustSelfSignedCerts { get; set; } = true;

    /// <summary>True when this profile points at a server installed/managed on this machine.</summary>
    public bool IsLocallyManaged { get; set; }

    public LocalServerSettings Local { get; set; } = new();

    public ServerProfile Clone() => new()
    {
        Id = Id,
        Name = Name,
        Host = Host,
        HttpsPort = HttpsPort,
        QueryPort = QueryPort,
        AuthToken = AuthToken,
        TrustSelfSignedCerts = TrustSelfSignedCerts,
        IsLocallyManaged = IsLocallyManaged,
        Local = new LocalServerSettings
        {
            InstallDir = Local.InstallDir,
            SteamCmdPath = Local.SteamCmdPath,
            BetaBranch = Local.BetaBranch,
            GamePort = Local.GamePort,
            QueryPort = Local.QueryPort,
            BeaconPort = Local.BeaconPort,
            MultiHome = Local.MultiHome,
            ExtraArgs = Local.ExtraArgs,
        }
    };
}
