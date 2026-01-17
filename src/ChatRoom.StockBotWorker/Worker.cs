using System.Text;
using System.Text.Json;
using ChatRoom.StockBotWorker.Contracts;
using ChatRoom.StockBotWorker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatRoom.StockBotWorker;

public class Worker : BackgroundService
{
    private const string CommandsQueue = "stock.commands";
    private const string ResultsQueue = "stock.results";

    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _http;
    private readonly RabbitMqConnectionFactory _rmq;

    private IConnection? _conn;
    private IChannel? _channel;

    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Worker(ILogger<Worker> logger, IHttpClientFactory http, RabbitMqConnectionFactory rmq)
    {
        _logger = logger;
        _http = http;
        _rmq = rmq;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _conn = await _rmq.CreateConnectionAsync();
        _channel = await _conn.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(CommandsQueue, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(ResultsQueue, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var cmd = JsonSerializer.Deserialize<StockCommand>(Encoding.UTF8.GetString(ea.Body.ToArray()), _json);
                if (cmd is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    return;
                }

                var text = await GetQuoteText(cmd.StockCode, stoppingToken);

                var result = new StockResult(
                    RoomId: cmd.RoomId,
                    Text: text,
                    CorrelationId: cmd.CorrelationId,
                    TimeStamp: DateTime.UtcNow
                );

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result, _json));
                var props = new BasicProperties { Persistent = true, CorrelationId = result.CorrelationId };

                await _channel.BasicPublishAsync("", ResultsQueue, false, props, body, stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bot failed processing message");
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(CommandsQueue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    
    private async Task<string> GetQuoteText(string stockCode, CancellationToken ct)
    {
        var code = stockCode.Trim();
        var url = $"https://stooq.com/q/l/?s={Uri.EscapeDataString(code)}&f=sd2t2ohlcv&h&e=csv";

        try
        {
            var client = _http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var csv = await client.GetStringAsync(url, ct);
            var price = StooqCsvParser.TryParseClosePrice(csv);
            var upper = code.ToUpperInvariant();

            return price is not null
                ? $"{upper} quote is ${price} per share"
                : $"Could not retrieve quote for {upper}";
        }
        catch
        {
            return $"Could not retrieve quote for {code.ToUpperInvariant()}";
        }
    }
}
