using System.Collections.Generic;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Contains summary information about a completed SteamCMD run.
    /// </summary>
    public sealed record SteamCmdRunResult(
        int? ExitCode,
        bool WasKilled,
        IReadOnlyList<string> Lines,
        IReadOnlyList<SteamCmdProgress> ProgressUpdates,
        string Transcript);
}