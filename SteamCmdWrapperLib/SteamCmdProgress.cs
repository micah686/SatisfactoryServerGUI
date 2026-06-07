namespace SteamCmdWrapperLib
{
    /// <summary>
    /// Represents a parsed progress update from SteamCMD.  The
    /// original raw text is not stored here; only the parsed values.
    /// </summary>
    public sealed record SteamCmdProgress(
        string StateHex,
        string StateName,
        double Percent,
        long BytesDone,
        long BytesTotal);
}