using System.Globalization;

namespace ChatRoom.Shared.Parsing;

public static class StooqCsvParser
{
    public static decimal? TryParseClosePrice(string csv)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2) return null;

        var header = lines[0].Split(',', StringSplitOptions.TrimEntries);
        var row = lines[1].Split(',', StringSplitOptions.TrimEntries);
        if (header.Length != row.Length) return null;

        var closeIndex = Array.FindIndex(header, h => h.Equals("Close", StringComparison.OrdinalIgnoreCase));
        if (closeIndex < 0) return null;

        var value = row[closeIndex];
        if (string.IsNullOrWhiteSpace(value) || value.Equals("N/D", StringComparison.OrdinalIgnoreCase)) return null;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}