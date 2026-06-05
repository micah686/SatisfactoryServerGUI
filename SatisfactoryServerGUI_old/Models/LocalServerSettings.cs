using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SatisfactoryServerGUI.Models;

/// <summary>
/// Settings for a locally-installed dedicated server that this app can install,
/// update (via SteamCMD) and launch.
/// </summary>
public class LocalServerSettings
{
    /// <summary>Directory the dedicated server is (or will be) installed into.</summary>
    public string InstallDir { get; set; } = string.Empty;

    /// <summary>Path to the steamcmd executable (steamcmd.exe / steamcmd / steamcmd.sh).</summary>
    public string SteamCmdPath { get; set; } = string.Empty;

    /// <summary>SteamCMD beta branch (e.g. "public" or "experimental").</summary>
    public string BetaBranch { get; set; } = "public";

    public int GamePort { get; set; } = 7777;
    public int QueryPort { get; set; } = 7777;
    public int BeaconPort { get; set; } = 15000;
    public string? MultiHome { get; set; }

    /// <summary>Free-text extra launch arguments appended verbatim.</summary>
    public string ExtraArgs { get; set; } = string.Empty;

    /// <summary>Satisfactory dedicated server Steam app id.</summary>
    public const string AppId = "1690800";

    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Resolves the FactoryServer launcher path, probing the common SteamCMD layouts.
    /// Returns the first candidate that exists, or the most likely path if none exist yet.
    /// </summary>
    public string ResolveServerExecutable()
    {
        var exeName = IsWindows ? "FactoryServer.exe" : "FactoryServer.sh";
        var candidates = new[]
        {
            Path.Combine(InstallDir, exeName),
            Path.Combine(InstallDir, "Engine", "Binaries", "Win64", exeName),
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c))
                return c;
        }

        return candidates[0];
    }

    public string BuildLaunchArgs()
    {
        var sb = new StringBuilder();
        sb.Append("-Port=").Append(GamePort);
        sb.Append(" -ServerQueryPort=").Append(QueryPort);
        sb.Append(" -BeaconPort=").Append(BeaconPort);
        if (!string.IsNullOrWhiteSpace(MultiHome))
            sb.Append(" -multihome=").Append(MultiHome);
        if (!string.IsNullOrWhiteSpace(ExtraArgs))
            sb.Append(' ').Append(ExtraArgs.Trim());
        return sb.ToString();
    }
}
