namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Holds a piece of normalized output with its kind.  Used
    /// internally by TerminalOutputNormalizer.
    /// </summary>
    internal sealed record NormalizedTerminalOutput(SteamCmdOutputKind Kind, string Text);
}