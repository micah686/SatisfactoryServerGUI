using System.Text.RegularExpressions;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Provides static methods for removing ANSI escape sequences and
    /// control characters from terminal output.  This allows
    /// downstream parsing to work on plain text.
    /// </summary>
    internal static class TerminalTextSanitizer
    {
        private static readonly Regex AnsiControlSequenceRegex = new(
            @"(?:\u001B\[|\u009B)[0-?]*[ -/]*[@-~]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Removes ANSI escape sequences from the given text.
        /// Returns an empty string when input is null.
        /// </summary>
        public static string Clean(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return AnsiControlSequenceRegex.Replace(text, string.Empty);
        }
    }
}
