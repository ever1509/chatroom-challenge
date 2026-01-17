using RabbitMQ.Client;

namespace ChatRoom.StockBotWorker.Services;

public class RabbitMqConnectionFactory
{
    private readonly ConnectionFactory _connectionFactory;

    public RabbitMqConnectionFactory(IConfiguration configuration)
    {
        _connectionFactory = new ConnectionFactory()
        {
            HostName = configuration["RabbitMq:HostName"],
            Port = int.Parse(configuration["RabbitMq:Port"] ?? "5672"),
            UserName = configuration["RabbitMq:UserName"],
            Password = configuration["RabbitMq:Password"],
        };
    }
    public async Task<IConnection> CreateConnectionAsync()
    {
        return await _connectionFactory.CreateConnectionAsync();
    }
}