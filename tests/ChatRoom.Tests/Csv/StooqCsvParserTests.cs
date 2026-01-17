using ChatRoom.Shared.Parsing;
using FluentAssertions;

namespace ChatRoom.Tests.Csv;

public class StooqCsvParserTests
{
    [Fact]
    public void Parses_valid_close_price()
    {
        var csv = """
                  Symbol,Date,Time,Open,High,Low,Close,Volume
                  AAPL.US,2024-01-01,22:00,100,110,90,255.53,100000
                  """;

        var price = StooqCsvParser.TryParseClosePrice(csv);

        price.Should().Be(255.53m);
    }

    [Fact]
    public void Returns_null_for_ND_price()
    {
        var csv = """
                  Symbol,Date,Time,Open,High,Low,Close,Volume
                  AAPL.US,2024-01-01,22:00,100,110,90,N/D,100000
                  """;

        var price = StooqCsvParser.TryParseClosePrice(csv);

        price.Should().BeNull();
    }
}