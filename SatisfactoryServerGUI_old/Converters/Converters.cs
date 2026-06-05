using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SatisfactoryServerGUI.Models;

namespace SatisfactoryServerGUI.Converters;

/// <summary>true -> SuccessBrush(green), false -> ErrorBrush(red).</summary>
public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var on = value is true;
        return Brush(on ? "#4CAF50" : "#F44336");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    internal static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public class ConnectionStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = (value as ConnectionStatus?) switch
        {
            ConnectionStatus.Connected => "#4CAF50",
            ConnectionStatus.Connecting => "#FF9800",
            ConnectionStatus.Error => "#F44336",
            _ => "#9E9E9E",
        };
        return BoolToBrushConverter.Brush(color);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class SeverityToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = (value as ConsoleEntrySeverity?) switch
        {
            ConsoleEntrySeverity.Error => "#F44336",
            ConsoleEntrySeverity.Warning => "#FF9800",
            ConsoleEntrySeverity.Command => "#2196F3",
            ConsoleEntrySeverity.Response => "#4CAF50",
            _ => "#DDDDDD",
        };
        return BoolToBrushConverter.Brush(color);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Seconds (int/long) -> "1d 3h 12m 04s" style duration.</summary>
public class SecondsToDurationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        long seconds = value switch
        {
            int i => i,
            long l => l,
            float f => (long)f,
            double d => (long)d,
            _ => 0,
        };

        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Minutes}m {ts.Seconds}s";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
}

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value != null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
