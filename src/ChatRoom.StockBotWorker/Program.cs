using ChatRoom.StockBotWorker;
using ChatRoom.StockBotWorker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
