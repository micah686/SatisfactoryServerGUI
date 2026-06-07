#if WINDOWS
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteamCMD.ConPTY;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Windows specific implementation using the SteamCMD.ConPTY
    /// library.  Provides real time output capture by hosting
    /// steamcmd in a ConPTY and exposing OutputDataReceived events.
    /// </summary>
    internal sealed class WindowsSteamCmdSession : ISteamCmdSession
    {
        private readonly SteamCmdWrapperOptions _options;
        private SteamCMDConPTY? _conPty;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private int _exitRaised;
        private bool _started;

        public WindowsSteamCmdSession(SteamCmdWrapperOptions options)
        {
            _options = options;
        }

        public event EventHandler<string>? RawOutputReceived;
        public event EventHandler<SteamCmdExitedEventArgs>? Exited;

        public Task StartAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            if (_started) throw new InvalidOperationException("Session already started.");
            // Build the command line string by joining arguments with spaces.
            var argString = arguments == null || arguments.Count == 0 ? string.Empty : string.Join(" ", arguments);
            var conPty = new SteamCMDConPTY(_options.SteamCmdPath, argString);
            conPty.OutputDataReceived += (sender, data) =>
            {
                if (!string.IsNullOrEmpty(data)) RawOutputReceived?.Invoke(this, data);
            };
            conPty.Exited += (sender, code) =>
            {
                RaiseExitedOnce(new SteamCmdExitedEventArgs(code, false));
            };
            conPty.Start();
            _conPty = conPty;
            _started = true;
            return Task.CompletedTask;
        }

        public async Task SendLineAsync(string command, CancellationToken cancellationToken)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var conPty = _conPty ?? throw new InvalidOperationException("Session not started.");
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                conPty.WriteLine(command);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken)
        {
            // ConPTY supports resizing but SteamCMD.ConPTY library may not expose it.  No‑op.
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            var conPty = _conPty;
            if (conPty == null) return Task.CompletedTask;
            try
            {
                conPty.Kill();
            }
            catch
            {
                // ignore
            }
            try
            {
                conPty.Dispose();
            }
            catch
            {
                // ignore
            }
            _conPty = null;
            RaiseExitedOnce(new SteamCmdExitedEventArgs(null, true));
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
            _writeLock.Dispose();
        }

        private void RaiseExitedOnce(SteamCmdExitedEventArgs args)
        {
            if (Interlocked.Exchange(ref _exitRaised, 1) != 0) return;
            Exited?.Invoke(this, args);
        }
    }
}
#endif