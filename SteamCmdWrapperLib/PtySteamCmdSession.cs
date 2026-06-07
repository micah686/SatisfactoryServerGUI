using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pty.Net;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Implements ISteamCmdSession using the cross‑platform PTY API
    /// provided by the sch.pty.net (vs-pty.net) library.  Works on
    /// Linux, macOS and Windows.  On Windows it defaults to using
    /// ConPTY internally unless ForceWinPty is set in the options.
    /// </summary>
    internal sealed class PtySteamCmdSession : ISteamCmdSession
    {
        private readonly SteamCmdWrapperOptions _options;
        private IPtyConnection? _connection;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly object _sync = new();
        private CancellationTokenSource? _readCts;
        private Task? _readTask;
        private bool _stopRequested;
        private int _exitRaised;

        public PtySteamCmdSession(SteamCmdWrapperOptions options)
        {
            _options = options;
        }

        public event EventHandler<string>? RawOutputReceived;
        public event EventHandler<SteamCmdExitedEventArgs>? Exited;

        public async Task StartAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            lock (_sync)
            {
                if (_connection != null) throw new InvalidOperationException("Session already started.");
                _exitRaised = 0;
                _stopRequested = false;
            }
            // Merge environment variables
            var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _options.Environment)
            {
                env[kvp.Key] = kvp.Value;
            }
            var ptyOptions = new PtyOptions
            {
                Name = _options.TerminalName,
                Cols = _options.TerminalColumns,
                Rows = _options.TerminalRows,
                Cwd = _options.EffectiveWorkingDirectory,
                App = _options.SteamCmdPath,
                CommandLine = arguments.ToArray(),
                Environment = env,
                ForceWinPty = _options.ForceWinPty,
                VerbatimCommandLine = false
            };
            var connection = await PtyProvider.SpawnAsync(ptyOptions, cancellationToken).ConfigureAwait(false);
            connection.ProcessExited += OnProcessExited;
            lock (_sync)
            {
                _connection = connection;
                _readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _readTask = Task.Run(() => ReadLoopAsync(connection, _readCts.Token), CancellationToken.None);
            }
        }

        public async Task SendLineAsync(string command, CancellationToken cancellationToken)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            IPtyConnection conn;
            lock (_sync)
            {
                conn = _connection ?? throw new InvalidOperationException("Session has not been started.");
            }
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var bytes = _options.OutputEncoding.GetBytes(command + "\r");
                await conn.WriterStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                await conn.WriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken)
        {
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
            IPtyConnection conn;
            lock (_sync)
            {
                conn = _connection ?? throw new InvalidOperationException("Session has not been started.");
            }
            conn.Resize(columns, rows);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            IPtyConnection? conn;
            CancellationTokenSource? readCts;
            Task? readTask;
            lock (_sync)
            {
                conn = _connection;
                readCts = _readCts;
                readTask = _readTask;
                _stopRequested = true;
            }
            if (conn == null) return;
            try
            {
                readCts?.Cancel();
                conn.Kill();
            }
            catch
            {
                // ignore errors killing process
            }
            if (readTask != null)
            {
                try
                {
                    await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(2), cancellationToken)).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }
            Cleanup();
            RaiseExitedOnce(new SteamCmdExitedEventArgs(TryGetExitCode(conn), true));
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
            _writeLock.Dispose();
        }

        private async Task ReadLoopAsync(IPtyConnection conn, CancellationToken cancellationToken)
        {
            var decoder = _options.OutputEncoding.GetDecoder();
            var byteBuffer = new byte[4096];
            var charBuffer = new char[_options.OutputEncoding.GetMaxCharCount(byteBuffer.Length)];
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var count = await conn.ReaderStream.ReadAsync(byteBuffer, 0, byteBuffer.Length, cancellationToken).ConfigureAwait(false);
                    if (count <= 0) break;
                    var charsRead = decoder.GetChars(byteBuffer, 0, count, charBuffer, 0, flush: false);
                    if (charsRead > 0)
                    {
                        RawOutputReceived?.Invoke(this, new string(charBuffer, 0, charsRead));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected
            }
            catch (Exception ex)
            {
                RawOutputReceived?.Invoke(this, $"[SteamCmdWrapper read error] {ex.Message}\n");
            }
            finally
            {
                if (!_stopRequested)
                {
                    RaiseExitedOnce(new SteamCmdExitedEventArgs(TryGetExitCode(conn), false));
                }
            }
        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            IPtyConnection? conn;
            lock (_sync)
            {
                conn = _connection;
            }
            if (conn != null)
            {
                RaiseExitedOnce(new SteamCmdExitedEventArgs(TryGetExitCode(conn), false));
            }
        }

        private int? TryGetExitCode(IPtyConnection conn)
        {
            try
            {
                return conn.ExitCode;
            }
            catch
            {
                return null;
            }
        }

        private void RaiseExitedOnce(SteamCmdExitedEventArgs args)
        {
            if (Interlocked.Exchange(ref _exitRaised, 1) != 0) return;
            try
            {
                Exited?.Invoke(this, args);
            }
            finally
            {
                Cleanup();
            }
        }

        private void Cleanup()
        {
            IPtyConnection? conn;
            CancellationTokenSource? readCts;
            lock (_sync)
            {
                conn = _connection;
                readCts = _readCts;
                _connection = null;
                _readCts = null;
                _readTask = null;
            }
            try { readCts?.Cancel(); readCts?.Dispose(); } catch { }
            try { conn?.Dispose(); } catch { }
        }
    }
}