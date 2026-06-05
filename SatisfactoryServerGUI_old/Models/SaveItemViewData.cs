using System;
using SatisfactorySdk.DTO;

namespace SatisfactoryServerGUI.Models;

/// <summary>Flattened save-file row for the Saves DataGrid (wraps the SDK SaveHeader).</summary>
public class SaveItemViewData
{
    public string SessionName { get; init; } = string.Empty;
    public string SaveName { get; init; } = string.Empty;
    public string MapName { get; init; } = string.Empty;
    public long PlayDurationSeconds { get; init; }
    public DateTime SaveDate { get; init; }
    public bool IsModded { get; init; }
    public bool IsCreative { get; init; }

    public static SaveItemViewData FromHeader(SaveHeader header)
    {
        DateTime date;
        try { date = header.SaveDate; }
        catch { date = DateTime.MinValue; }

        return new SaveItemViewData
        {
            SessionName = header.SessionName ?? string.Empty,
            SaveName = header.Name ?? string.Empty,
            MapName = header.MapName ?? string.Empty,
            PlayDurationSeconds = header.PlayDurationSections,
            SaveDate = date,
            IsModded = header.IsModded,
            IsCreative = header.IsCreativeMode,
        };
    }
}
