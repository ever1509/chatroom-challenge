using System.Text;
using System.Text.Json;
using ChatRoom.Api.Contracts;
using RabbitMQ.Client;

namespace ChatRoom.Api.Services;

public class StockCommandPublisher : IAsyncDisposable
{
    public const string CommandQueue = "stock.commands";
    private readonly RabbitMqConnectionFactory _factory;
    private IConnection _connection;
    private IChannel _channel;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    public StockCommandPublisher(RabbitMqConnectionFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if(_channel is not null) return;
        
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if(_channel is not null) return;

            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
           await _channel.QueueDeclareAsync(
                queue: CommandQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null, cancellationToken: cancellationToken);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task PublishAsync(StockCommand command, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);
        
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command, _json));

        var props = new BasicProperties
        {
            Persistent = true,
            CorrelationId = command.CorrelationId
        };

        await _channel!.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: CommandQueue,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken
        );
    }

    public async ValueTask DisposeAsync()
    {
       if(_channel is not null) await _channel.CloseAsync();
       if(_connection is not null) await _connection.CloseAsync();
       _initLock.Dispose();
    }
}