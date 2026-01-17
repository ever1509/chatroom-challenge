using System.Text;
using System.Text.Json;
using ChatRoom.Api.Contracts;
using ChatRoom.Api.Data;
using ChatRoom.Api.Hubs;
using ChatRoom.Api.Models;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatRoom.Api.Services;

public class StockResultConsumer : BackgroundService, IAsyncDisposable
{
    private const string ResultsQueue = "stock.results";

    private readonly RabbitMqConnectionFactory _rmqFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ChatHub> _hub;
    private readonly ILogger<StockResultConsumer> _logger;

    private IConnection? _conn;
    private IChannel? _channel;

    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StockResultConsumer(
        RabbitMqConnectionFactory rmqFactory,
        IServiceScopeFactory scopeFactory,
        IHubContext<ChatHub> hub,
        ILogger<StockResultConsumer> logger)
    {
        _rmqFactory = rmqFactory;
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _conn = await _rmqFactory.CreateConnectionAsync();
        _channel = await _conn.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: ResultsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var result = JsonSerializer.Deserialize<StockResult>(json, _json);

                if (result is null)
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    return;
                }

                // Persist bot message
                Message msg;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

                    msg = new Message
                    {
                        RoomId = result.RoomId,
                        UserId = "bot",
                        UserName = "StockBot",
                        Text = result.Text,
                        TimeStamp = result.TimeStamp,
                        IsBot = true
                    };

                    db.ChatMessages.Add(msg);
                    await db.SaveChangesAsync(stoppingToken);
                }

                // Broadcast to the correct room group
                await _hub.Clients.Group($"room:{result.RoomId}").SendAsync("newMessage", new
                {
                    msg.Id,
                    msg.RoomId,
                    msg.UserName,
                    msg.Text,
                    msg.TimeStamp,
                    msg.IsBot
                }, stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing stock result message");
                // Ack to avoid poison-loop. (Bonus later: dead-letter)
                await _channel!.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: ResultsQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();

        if (_conn is not null)
            await _conn.CloseAsync();

        base.Dispose();
    }

}