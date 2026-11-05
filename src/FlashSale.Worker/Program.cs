using FlashSale.Application;
using FlashSale.Infrastructure;
using FlashSale.Worker;
using FlashSale.Worker.Consumers;
using RabbitMQ.Client;

// Đọc cấu hình từ environment variable (không hardcode secret)
var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Đăng ký Infrastructure (PostgreSQLPrimary, Redis, RabbitMqPublisherService, OrderRepository, OutboxRepository, UnitOfWork, Identity)
builder.Services.AddInfrastructure(builder.Configuration);

// Đăng ký Application (IOrderService, IOutboxService, IOutboxPublisher, validators)
builder.Services.AddApplication();

// RabbitMQ connection & channel (singleton, cung nhom voi ApiGateway)
var rabbitMqConnectionString = builder.Configuration["RABBITMQ_CONNECTION_STRING"]
    ?? "amqp://guest:guest@localhost:5672/";
var factory = new ConnectionFactory
{
    Uri = new Uri(rabbitMqConnectionString),
    AutomaticRecoveryEnabled = false,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
};
var connection = factory.CreateConnection();
builder.Services.AddSingleton<IConnection>(connection);
builder.Services.AddSingleton<IModel>(connection.CreateModel());

// Dich vu tieu thu message OrderCreated tu Queue
builder.Services.AddScoped<OrderCreatedConsumerService>();

// Background Service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
