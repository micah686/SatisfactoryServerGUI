using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// High‑level wrapper around Valve's steamcmd that hides the
    /// complexity of pseudo terminal management, CR/LF parsing,
    /// ANSI stripping and progress parsing.  Depending on the operating
    /// system and options, it selects a suitable low‑level session
    /// implementation such as a PTY session (cross‑platform via
    /// sch.pty.net) or a native Windows ConPTY session (via
    /// SteamCMD.ConPTY).  It exposes events for cleaned output lines,
    /// carriage‑return status messages and parsed progress updates, and
    /// provides convenience methods for starting, stopping and
    /// running a full SteamCMD command.
    /// </summary>
    public sealed class SteamCmdWrapper : IAsyncDisposable
    {
        private readonly ISteamCmdSession _session;
        private readonly TerminalOutputNormalizer _normalizer;
        private readonly SteamCmdWrapperOptions _options;
        private readonly object _sync = new();
        private readonly StringBuilder _transcript = new();
        private TaskCompletionSource<SteamCmdExitedEventArgs> _exitTcs = CreateExitTcs();
        private bool _started;
        private bool _finished;

        /// <summary>
        /// Creates a new wrapper with default options for the
        /// specified steamcmd path.
        /// </summary>
        /// <param name="steamCmdPath">Absolute path to the steamcmd
        /// executable or shell script.</param>
        public SteamCmdWrapper(string steamCmdPath)
            : this(new SteamCmdWrapperOptions(steamCmdPath))
        {
        }

        /// <summary>
        /// Creates a new wrapper with the specified options.
        /// </summary>
        public SteamCmdWrapper(SteamCmdWrapperOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();
            _normalizer = new TerminalOutputNormalizer(options.EmitEmptyLines);

            // Choose a platform-specific implementation. Linux uses managed
            // redirected process streams because the native PTY package can
            // terminate the host process on current Linux runtimes.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && options.UseWindowsConPty)
            {
#if WINDOWS
                _session = new WindowsSteamCmdSession(options);
#else
                throw new PlatformNotSupportedException(
                    "The Windows ConPTY session is unavailable; compile with the WINDOWS symbol and reference SteamCMD.ConPTY");
#endif
            }
            else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _session = new ProcessSteamCmdSession(options);
            }
            else
            {
                _session = new PtySteamCmdSession(options);
            }

            // Wire low‑level session events to normalizer and public events.
            _session.RawOutputReceived += HandleRawOutput;
            _session.Exited += HandleSessionExited;
            _normalizer.LineReceived += line => HandleNormalizedOutput(SteamCmdOutputKind.Line, line);
            _normalizer.StatusReceived += status => HandleNormalizedOutput(SteamCmdOutputKind.Status, status);
        }

        /// <summary>
        /// Gets the options used by this wrapper.  These options are
        /// immutable after the wrapper has been constructed.
        /// </summary>
        public SteamCmdWrapperOptions Options => _options;

        /// <summary>
        /// Indicates whether steamcmd has been started and has not
        /// yet exited or been stopped.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_sync)
                {
                    return _started && !_finished;
                }
            }
        }

        /// <summary>
        /// Returns the full accumulated transcript of output seen
        /// from SteamCMD in its cleaned form.  Useful for logging.
        /// </summary>
        public string Transcript
        {
            get
            {
                lock (_sync)
                {
                    return _transcript.ToString();
                }
            }
        }

        /// <summary>
        /// Raised for each raw chunk of data read from the PTY or
        /// ConPTY.  Most callers should use LineReceived,
        /// StatusReceived or ProgressReceived instead.
        /// </summary>
        public event EventHandler<string>? RawOutputReceived;

        /// <summary>
        /// Raised for each cleaned line or status message.  See
        /// LineReceived and StatusReceived for more specific events.
        /// </summary>
        public event EventHandler<SteamCmdOutputEventArgs>? OutputReceived;

        /// <summary>
        /// Raised for each normal newline‑terminated output line.
        /// </summary>
        public event EventHandler<SteamCmdLineEventArgs>? LineReceived;

        /// <summary>
        /// Raised for each carriage‑return terminated status update.
        /// </summary>
        public event EventHandler<SteamCmdLineEventArgs>? StatusReceived;

        /// <summary>
        /// Raised when a progress message is parsed from either a line
        /// or status update.
        /// </summary>
        public event EventHandler<SteamCmdProgressEventArgs>? ProgressReceived;

        /// <summary>
        /// Raised when the underlying session exits.  Provides the
        /// exit code and whether the process was killed by the wrapper.
        /// </summary>
        public event EventHandler<SteamCmdExitedEventArgs>? Exited;

        /// <summary>
        /// Starts SteamCMD with the given arguments.  Arguments should
        /// be provided exactly as they would be typed in a shell (no
        /// quoting or escaping performed).  The session continues
        /// running until StopAsync or WaitForExitAsync is called.
        /// </summary>
        public async Task StartAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            lock (_sync)
            {
                if (_started) throw new InvalidOperationException("The wrapper has already been started.");
                _started = true;
                _finished = false;
                _exitTcs = CreateExitTcs();
                _transcript.Clear();
            }
            _normalizer.Reset();
            await _session.StartAsync(arguments.ToArray(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Convenience overload to start using a params array.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken, params string[] arguments) => StartAsync(arguments, cancellationToken);

        /// <summary>
        /// Sends an interactive command line to the running session.
        /// A carriage return is appended automatically.  Throws if
        /// steamcmd has not been started yet.
        /// </summary>
        public Task SendLineAsync(string command, CancellationToken cancellationToken = default) => _session.SendLineAsync(command, cancellationToken);

        /// <summary>
        /// Resizes the underlying terminal.  Only meaningful for
        /// interactive sessions; not required for one‑shot commands.
        /// </summary>
        public Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken = default) => _session.ResizeAsync(columns, rows, cancellationToken);

        /// <summary>
        /// Stops the running session by killing the process.  It is
        /// safe to call this if the session is not running.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken = default) => _session.StopAsync(cancellationToken);

        /// <summary>
        /// Runs SteamCMD to completion with the specified arguments and
        /// waits for it to exit.  Returns detailed results including
        /// exit code, transcript, progress updates and captured lines.
        /// </summary>
        public async Task<SteamCmdRunResult> ExecuteAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
        {
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            await StartAsync(arguments, cancellationToken).ConfigureAwait(false);
            var exitArgs = await WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            // All cleaned lines and progress updates are captured via events.
            return new SteamCmdRunResult(
                exitArgs.ExitCode,
                exitArgs.WasKilled,
                _normalizer.GetLines(),
                _normalizer.GetProgresses(),
                Transcript
            );
        }

        /// <summary>
        /// Waits for the session to exit and returns the exit data.
        /// </summary>
        public Task<SteamCmdExitedEventArgs> WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            var task = _exitTcs.Task;
            if (!cancellationToken.CanBeCanceled) return task;
            return WaitForExitWithCancellationAsync(task, cancellationToken);
        }

        /// <summary>
        /// Disposes of the wrapper and stops the session if it is still running.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _session.DisposeAsync().ConfigureAwait(false);
        }

        private void HandleRawOutput(object? sender, string chunk)
        {
            RawOutputReceived?.Invoke(this, chunk);
            _normalizer.Push(chunk);
        }

        private void HandleNormalizedOutput(SteamCmdOutputKind kind, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            lock (_sync)
            {
                _transcript.AppendLine(text);
            }
            var args = new SteamCmdOutputEventArgs(kind, text);
            OutputReceived?.Invoke(this, args);
            if (kind == SteamCmdOutputKind.Line)
                LineReceived?.Invoke(this, new SteamCmdLineEventArgs(text));
            else
                StatusReceived?.Invoke(this, new SteamCmdLineEventArgs(text));
            if (SteamCmdProgressParser.TryParse(text, out var progress))
            {
                ProgressReceived?.Invoke(this, new SteamCmdProgressEventArgs(progress));
                _normalizer.RecordProgress(progress);
            }
        }

        private void HandleSessionExited(object? sender, SteamCmdExitedEventArgs e)
        {
            // Flush any pending buffered data.
            foreach (var output in _normalizer.Flush())
            {
                HandleNormalizedOutput(output.Kind, output.Text);
            }
            lock (_sync)
            {
                if (_finished) return;
                _finished = true;
            }
            Exited?.Invoke(this, e);
            _exitTcs.TrySetResult(e);
        }

        private static Task<SteamCmdExitedEventArgs> WaitForExitWithCancellationAsync(Task<SteamCmdExitedEventArgs> waitTask, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled) return waitTask;
            var tcs = new TaskCompletionSource<SteamCmdExitedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            waitTask.ContinueWith(t =>
            {
                registration.Dispose();
                if (t.IsCanceled) tcs.TrySetCanceled();
                else if (t.IsFaulted) tcs.TrySetException(t.Exception!);
                else tcs.TrySetResult(t.Result);
            }, TaskScheduler.Default);
            return tcs.Task;
        }

        private static TaskCompletionSource<SteamCmdExitedEventArgs> CreateExitTcs() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
