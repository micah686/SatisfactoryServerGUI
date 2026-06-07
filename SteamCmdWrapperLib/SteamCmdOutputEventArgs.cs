using System;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Distinguishes between normal line output and carriage return
    /// status updates.  SteamCmdOutputKind.Line corresponds to
    /// newline‑terminated lines; SteamCmdOutputKind.Status corresponds
    /// to carriage return updates.
    /// </summary>
    public enum SteamCmdOutputKind
    {
        Line,
        Status
    }

    /// <summary>
    /// Base class for output events carrying the kind and text.
    /// </summary>
    public class SteamCmdOutputEventArgs : EventArgs
    {
        public SteamCmdOutputEventArgs(SteamCmdOutputKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }
        public SteamCmdOutputKind Kind { get; }
        public string Text { get; }
    }

    /// <summary>
    /// Event args for a single line or status message.
    /// </summary>
    public class SteamCmdLineEventArgs : EventArgs
    {
        public SteamCmdLineEventArgs(string text)
        {
            Text = text;
        }
        public string Text { get; }
    }

    /// <summary>
    /// Event args carrying a parsed progress update.
    /// </summary>
    public class SteamCmdProgressEventArgs : EventArgs
    {
        public SteamCmdProgressEventArgs(SteamCmdProgress progress)
        {
            Progress = progress;
        }
        public SteamCmdProgress Progress { get; }
    }
}