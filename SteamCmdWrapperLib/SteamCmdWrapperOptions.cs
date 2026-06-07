using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Encapsulates configuration options for SteamCmdWrapper.  These
    /// options control how steamcmd is launched, the size of the
    /// pseudo terminal, environment variables and whether to use the
    /// native Windows ConPTY implementation on Windows.
    /// </summary>
    public sealed class SteamCmdWrapperOptions
    {
        /// <summary>
        /// Creates options for the given steamcmd path.  At a minimum
        /// the path must be provided; other properties have sensible
        /// defaults.
        /// </summary>
        public SteamCmdWrapperOptions(string steamCmdPath)
        {
            SteamCmdPath = steamCmdPath;
        }

        /// <summary>
        /// Absolute path to the steamcmd executable (on Windows) or
        /// shell script (on Linux).  Required.
        /// </summary>
        public string SteamCmdPath { get; }

        /// <summary>
        /// Working directory for steamcmd.  Defaults to the directory
        /// containing SteamCmdPath if not specified.
        /// </summary>
        public string? WorkingDirectory { get; init; }

        /// <summary>
        /// Number of columns in the pseudo terminal.  Default is 160.
        /// </summary>
        public int TerminalColumns { get; init; } = 160;

        /// <summary>
        /// Number of rows in the pseudo terminal.  Default is 40.
        /// </summary>
        public int TerminalRows { get; init; } = 40;

        /// <summary>
        /// Name of the terminal.  Used by PTY libraries; default is
        /// "SteamCMD".  See PtyOptions.Name.
        /// </summary>
        public string TerminalName { get; init; } = "SteamCMD";

        /// <summary>
        /// When true on Windows, instructs the wrapper to use the
        /// SteamCMD.ConPTY based implementation instead of the
        /// cross‑platform sch.pty.net implementation.  This is only
        /// meaningful on Windows; on other platforms it is ignored.
        /// </summary>
        public bool UseWindowsConPty { get; init; }

        /// <summary>
        /// When set on Windows and using sch.pty.net, forces the
        /// underlying vs-pty.net library to use the legacy WinPTY
        /// backend instead of ConPTY.  Typically only needed on older
        /// Windows versions.
        /// </summary>
        public bool ForceWinPty { get; init; }

        /// <summary>
        /// Controls whether empty lines (blank or whitespace) are
        /// emitted as Line events.  Defaults to false.
        /// </summary>
        public bool EmitEmptyLines { get; init; }

        /// <summary>
        /// Encoding used to decode output from steamcmd.  Defaults to
        /// UTF‑8 without BOM.  Changing this may be necessary if
        /// steamcmd prints text in a different encoding.
        /// </summary>
        public Encoding OutputEncoding { get; init; } = new UTF8Encoding(false, false);

        /// <summary>
        /// Additional environment variables to pass to steamcmd.  These
        /// are merged with the process environment.  Do not include
        /// variables that conflict with the terminal operation.
        /// </summary>
        public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();

        /// <summary>
        /// Returns the effective working directory by taking
        /// WorkingDirectory if specified or falling back to the
        /// directory containing SteamCmdPath or the current directory.
        /// </summary>
        internal string EffectiveWorkingDirectory
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(WorkingDirectory)) return WorkingDirectory!;
                var dir = Path.GetDirectoryName(SteamCmdPath);
                return string.IsNullOrWhiteSpace(dir) ? Directory.GetCurrentDirectory() : dir;
            }
        }

        /// <summary>
        /// Validates the options; throws exceptions if mandatory
        /// settings are missing or invalid.  Called internally by
        /// SteamCmdWrapper.
        /// </summary>
        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(SteamCmdPath))
                throw new ArgumentException("SteamCmdPath must not be empty.", nameof(SteamCmdPath));
            if (TerminalColumns <= 0) throw new ArgumentOutOfRangeException(nameof(TerminalColumns));
            if (TerminalRows <= 0) throw new ArgumentOutOfRangeException(nameof(TerminalRows));
        }
    }
}