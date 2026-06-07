using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Abstraction over a running SteamCMD process.  Concrete
    /// implementations provide platform specific behavior, such as
    /// using a PTY on Unix or ConPTY on Windows.  The wrapper
    /// interacts with this interface and is agnostic to the
    /// underlying implementation.
    /// </summary>
    internal interface ISteamCmdSession : IAsyncDisposable
    {
        /// <summary>
        /// Raised when raw output data is available.  Text may be
        /// chunked arbitrarily and include control codes.
        /// </summary>
        event EventHandler<string>? RawOutputReceived;

        /// <summary>
        /// Raised when the SteamCMD process exits.  Provides the
        /// exit code and whether the process was killed by the wrapper.
        /// </summary>
        event EventHandler<SteamCmdExitedEventArgs>? Exited;

        /// <summary>
        /// Starts the SteamCMD process with the given arguments.
        /// The arguments should not include the path itself.
        /// </summary>
        Task StartAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a line of text to the process, appending a newline or
        /// carriage return as appropriate.  Throws if the process has
        /// not been started.
        /// </summary>
        Task SendLineAsync(string command, CancellationToken cancellationToken);

        /// <summary>
        /// Resizes the terminal to the specified dimensions.  Only
        /// meaningful for PTY implementations.
        /// </summary>
        Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken);

        /// <summary>
        /// Stops the process by killing it.  It is safe to call even
        /// if the process is not running.
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken);
    }
}