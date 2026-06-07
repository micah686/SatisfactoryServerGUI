using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Parses progress updates emitted by SteamCMD.  These updates
    /// typically have the form "App state (0x....) SomeState,
    /// progress: N.NN (bytes/total)".
    /// </summary>
    public static class SteamCmdProgressParser
    {
        private static readonly Regex ProgressRegex = new(
            @"App state\s*\(0x(?<hex>[0-9a-fA-F]+)\)\s*(?<state>[^,]+),\s*progress:\s*(?<percent>\d+(?:\.\d+)?)\s*\((?<done>\d+)\s*/\s*(?<total>\d+)\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Attempts to parse a progress update from the provided text.
        /// Returns true on success and populates the progress record.
        /// </summary>
        public static bool TryParse(string? text, [NotNullWhen(true)] out SteamCmdProgress? progress)
        {
            progress = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var match = ProgressRegex.Match(text.Trim());
            if (!match.Success) return false;
            if (!double.TryParse(match.Groups["percent"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent)) return false;
            if (!long.TryParse(match.Groups["done"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var done)) return false;
            if (!long.TryParse(match.Groups["total"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var total)) return false;
            var stateHex = match.Groups["hex"].Value;
            var stateName = match.Groups["state"].Value.Trim();
            progress = new SteamCmdProgress(stateHex, stateName, percent, done, total);
            return true;
        }
    }
}