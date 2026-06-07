using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Runs SteamCMD using redirected process streams. This avoids the native
    /// PTY dependency on platforms where a pseudo terminal is not required.
    /// </summary>
    internal sealed class ProcessSteamCmdSession : ISteamCmdSession
    {
        private readonly SteamCmdWrapperOptions _options;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly object _sync = new();
        private readonly object _outputSync = new();
        private Process? _process;
        private Task? _completionTask;
        private bool _stopRequested;
        private int _exitRaised;

        public ProcessSteamCmdSession(SteamCmdWrapperOptions options)
        {
            _options = options;
        }

        public event EventHandler<string>? RawOutputReceived;
        public event EventHandler<SteamCmdExitedEventArgs>? Exited;

        public Task StartAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(arguments);
            cancellationToken.ThrowIfCancellationRequested();

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.SteamCmdPath,
                WorkingDirectory = _options.EffectiveWorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = _options.OutputEncoding,
                StandardErrorEncoding = _options.OutputEncoding
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            foreach (var variable in _options.Environment)
            {
                startInfo.Environment[variable.Key] = variable.Value;
            }

            var process = new Process
            {
                StartInfo = startInfo
            };

            lock (_sync)
            {
                if (_process != null)
                {
                    process.Dispose();
                    throw new InvalidOperationException("Session already started.");
                }

                _stopRequested = false;
                _exitRaised = 0;
                _process = process;
            }

            try
            {
                process.Start();
                var stdoutTask = ReadOutputAsync(process.StandardOutput);
                var stderrTask = ReadOutputAsync(process.StandardError);
                var completionTask = MonitorProcessAsync(process, stdoutTask, stderrTask);

                lock (_sync)
                {
                    _completionTask = completionTask;
                }
            }
            catch
            {
                Cleanup(process);
                throw;
            }

            return Task.CompletedTask;
        }

        public async Task SendLineAsync(string command, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            Process process;
            lock (_sync)
            {
                process = _process ?? throw new InvalidOperationException("Session has not been started.");
            }

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await process.StandardInput.WriteLineAsync(command.AsMemory(), cancellationToken).ConfigureAwait(false);
                await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
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
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Process? process;
            Task? completionTask;

            lock (_sync)
            {
                process = _process;
                completionTask = _completionTask;
                _stopRequested = true;
            }

            if (process == null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // The process exited between checking HasExited and killing it.
            }

            if (completionTask != null)
            {
                await completionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
            _writeLock.Dispose();
        }

        private async Task ReadOutputAsync(StreamReader reader)
        {
            var buffer = new char[4096];

            while (true)
            {
                var count = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);
                if (count == 0)
                {
                    return;
                }

                var output = new string(buffer, 0, count);
                lock (_outputSync)
                {
                    RawOutputReceived?.Invoke(this, output);
                }
            }
        }

        private async Task MonitorProcessAsync(Process process, Task stdoutTask, Task stderrTask)
        {
            try
            {
                await process.WaitForExitAsync().ConfigureAwait(false);
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

                bool wasKilled;
                lock (_sync)
                {
                    wasKilled = _stopRequested;
                }

                RaiseExitedOnce(new SteamCmdExitedEventArgs(process.ExitCode, wasKilled));
            }
            finally
            {
                Cleanup(process);
            }
        }

        private void RaiseExitedOnce(SteamCmdExitedEventArgs args)
        {
            if (Interlocked.Exchange(ref _exitRaised, 1) == 0)
            {
                Exited?.Invoke(this, args);
            }
        }

        private void Cleanup(Process process)
        {
            lock (_sync)
            {
                if (!ReferenceEquals(_process, process))
                {
                    return;
                }

                _process = null;
                _completionTask = null;
            }

            process.Dispose();
        }
    }
}
