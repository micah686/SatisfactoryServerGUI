using System;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Event args signaled when the SteamCMD process exits.  Provides
    /// the exit code when available and indicates if the wrapper
    /// forcibly killed the process.
    /// </summary>
    public sealed class SteamCmdExitedEventArgs : EventArgs
    {
        public SteamCmdExitedEventArgs(int? exitCode, bool wasKilled)
        {
            ExitCode = exitCode;
            WasKilled = wasKilled;
        }
        public int? ExitCode { get; }
        public bool WasKilled { get; }
    }
}