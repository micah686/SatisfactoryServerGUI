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
        private static readonly Regex AnsiRegex = new(
            @"[\u001B\u009B][[\]()#;?]*(?:(?:(?:[a-zA-Z\d]*(?:;[a-zA-Z\d]*)*)?\u0007)|(?:(?:\d{1,4}(?:;\d{0,4})*)?[\dA-PRZcf-ntqry=><~]))",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Removes ANSI escape sequences from the given text.
        /// Returns an empty string when input is null.
        /// </summary>
        public static string Clean(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return AnsiRegex.Replace(text, string.Empty);
        }
    }
}