namespace ChatRoom.Shared.Parsing;

public static class StockCommandParser
{
    public static bool TryParse(string text, out string code)
    {
        code = "";
        if (text is null) return false;

        var t = text.Trim();
        if (!t.StartsWith("/stock=", StringComparison.OrdinalIgnoreCase)) return false;

        var c = t["/stock=".Length..].Trim();
        if (string.IsNullOrWhiteSpace(c)) return false;

        code = c;
        return true;
    }
}