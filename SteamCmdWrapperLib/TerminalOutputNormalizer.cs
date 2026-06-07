using System;
using System.Collections.Generic;
using System.Text;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Aggregates raw terminal text into normalized lines or status
    /// messages by handling carriage returns, newlines and backspaces.
    /// It also collects lines and progress updates for run results.
    /// </summary>
    internal sealed class TerminalOutputNormalizer
    {
        private readonly bool _emitEmpty;
        private readonly StringBuilder _buffer = new();
        private bool _pendingCR;
        private readonly List<string> _lines = new();
        private readonly List<SteamCmdProgress> _progress = new();

        public TerminalOutputNormalizer(bool emitEmptyLines)
        {
            _emitEmpty = emitEmptyLines;
        }

        /// <summary>
        /// Event raised when a full line is available.
        /// </summary>
        public event Action<string>? LineReceived;

        /// <summary>
        /// Event raised when a status line (carriage return) is available.
        /// </summary>
        public event Action<string>? StatusReceived;

        /// <summary>
        /// Resets internal buffers and clears collected data.
        /// </summary>
        public void Reset()
        {
            _buffer.Clear();
            _pendingCR = false;
            _lines.Clear();
            _progress.Clear();
        }

        /// <summary>
        /// Pushes a chunk of raw text and emits events for any
        /// completed lines or status messages.  Returns a list of
        /// normalized outputs for convenience.
        /// </summary>
        public IReadOnlyList<NormalizedTerminalOutput> Push(string chunk)
        {
            var outputs = new List<NormalizedTerminalOutput>();
            foreach (var ch in chunk)
            {
                ProcessChar(ch, outputs);
            }
            // Emit events for each output immediately.
            foreach (var output in outputs)
            {
                Emit(output);
            }
            return outputs;
        }

        /// <summary>
        /// Flushes any pending buffered output when the session is
        /// terminating.  Emits events and returns the final outputs.
        /// </summary>
        public IReadOnlyList<NormalizedTerminalOutput> Flush()
        {
            var outputs = new List<NormalizedTerminalOutput>();
            if (_pendingCR)
            {
                outputs.Add(EmitStatus());
            }
            else if (_buffer.Length > 0)
            {
                outputs.Add(EmitLine());
            }
            // Emit events and return list.
            foreach (var output in outputs)
            {
                Emit(output);
            }
            return outputs;
        }

        /// <summary>
        /// Records a parsed progress update for later retrieval.
        /// </summary>
        public void RecordProgress(SteamCmdProgress progress) => _progress.Add(progress);

        /// <summary>
        /// Returns the collected lines.
        /// </summary>
        public IReadOnlyList<string> GetLines() => _lines.AsReadOnly();

        /// <summary>
        /// Returns the collected progress updates.
        /// </summary>
        public IReadOnlyList<SteamCmdProgress> GetProgresses() => _progress.AsReadOnly();

        private void ProcessChar(char ch, List<NormalizedTerminalOutput> outputs)
        {
            if (_pendingCR)
            {
                if (ch == '\n')
                {
                    outputs.Add(EmitLine());
                    _pendingCR = false;
                    return;
                }
                outputs.Add(EmitStatus());
                _pendingCR = false;
            }
            switch (ch)
            {
                case '\r':
                    _pendingCR = true;
                    break;
                case '\n':
                    outputs.Add(EmitLine());
                    break;
                case '\b':
                    if (_buffer.Length > 0) _buffer.Length--;
                    break;
                case '\0':
                case '\a':
                    // ignore NUL and BEL
                    break;
                default:
                    if (!char.IsControl(ch) || ch == '\t')
                    {
                        _buffer.Append(ch);
                    }
                    break;
            }
        }

        private NormalizedTerminalOutput EmitLine()
        {
            var text = TerminalTextSanitizer.Clean(_buffer.ToString()).TrimEnd();
            _buffer.Clear();
            if (!_emitEmpty && string.IsNullOrWhiteSpace(text))
            {
                return new NormalizedTerminalOutput(SteamCmdOutputKind.Line, string.Empty);
            }
            return new NormalizedTerminalOutput(SteamCmdOutputKind.Line, text);
        }

        private NormalizedTerminalOutput EmitStatus()
        {
            var text = TerminalTextSanitizer.Clean(_buffer.ToString()).TrimEnd();
            _buffer.Clear();
            if (!_emitEmpty && string.IsNullOrWhiteSpace(text))
            {
                return new NormalizedTerminalOutput(SteamCmdOutputKind.Status, string.Empty);
            }
            return new NormalizedTerminalOutput(SteamCmdOutputKind.Status, text);
        }

        private void Emit(NormalizedTerminalOutput output)
        {
            if (output.Text.Length == 0) return;
            if (output.Kind == SteamCmdOutputKind.Line)
            {
                _lines.Add(output.Text);
                LineReceived?.Invoke(output.Text);
            }
            else
            {
                StatusReceived?.Invoke(output.Text);
            }
        }
    }
}