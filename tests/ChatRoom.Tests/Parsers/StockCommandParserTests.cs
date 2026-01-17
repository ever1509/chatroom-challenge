using ChatRoom.Shared.Parsing;
using FluentAssertions;

namespace ChatRoom.Tests.Parsers;

public class StockCommandParserTests
{
    [Theory]
    [InlineData("/stock=aapl.us", "aapl.us")]
    [InlineData("/stock=GOOG", "GOOG")]
    [InlineData("/stock= msft ", "msft")]
    public void Valid_stock_commands_are_parsed(string input, string expected)
    {
        var ok = StockCommandParser.TryParse(input, out var code);

        ok.Should().BeTrue();
        code.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("/stock=")]
    [InlineData("/stock")]
    [InlineData("/stock aapl")]
    public void Invalid_commands_are_rejected(string input)
    {
        var ok = StockCommandParser.TryParse(input, out _);

        ok.Should().BeFalse();
    }
}