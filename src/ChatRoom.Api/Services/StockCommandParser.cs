using ChatRoom.Api.Contracts;

namespace ChatRoom.Api.Services;

public class StockCommandParser
{
    public static bool TryParse(string text, out string code)
    {
        code = string.Empty;
        
        if(text is null) return false;
        
        var t = text.Trim();
        
        if(!t.StartsWith("/stock=", StringComparison.OrdinalIgnoreCase)) return false;
        
        var c = t["/stock=".Length..].Trim();
        if(string.IsNullOrEmpty(c)) return false;
        
        code = c;
        return true;
    }
}