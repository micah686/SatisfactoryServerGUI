using System;

namespace SatisfactoryServerGUI.Models;

/// <summary>A single line in a console / log output view.</summary>
public class ConsoleEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string Text { get; init; } = string.Empty;
    public ConsoleEntrySeverity Severity { get; init; } = ConsoleEntrySeverity.Info;

    public string Display => $"[{Timestamp:HH:mm:ss}] {Text}";

    public ConsoleEntry() { }

    public ConsoleEntry(string text, ConsoleEntrySeverity severity = ConsoleEntrySeverity.Info)
    {
        Text = text;
        Severity = severity;
    }
}
