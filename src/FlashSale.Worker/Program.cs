using FlashSale.Application;
using FlashSale.Infrastructure;
using FlashSale.Worker.Consumers;
using FlashSale.Worker.Services;

namespace FlashSale.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        // Đọc cấu hình từ environment variable (không hardcode secret)
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddEnvironmentVariables();

        // Đăng ký Infrastructure (PostgreSQLPrimary, Redis, RabbitMQ provider/publisher lazy, repositories, Identity)
        builder.Services.AddInfrastructure(builder.Configuration);

        // Đăng ký Application (services, outbox, validators)
        builder.Services.AddApplication(builder.Configuration);

        // Dich vu tieu thu message OrderCreated tu Queue
        builder.Services.AddScoped<OrderCreatedConsumerService>();

        // Background Service
        builder.Services.AddHostedService<Worker>();

        // Background Service purge/archive bang Outbox dinh ky (moi 24 gio mot lan, NFR-06)
        builder.Services.AddHostedService<OutboxPurgeWorkerService>();

        // Background Service xuat phat Outbox Pending ra RabbitMQ dinh ky (moi 30 giay mot lan, FR-04, NFR-06)
        builder.Services.AddHostedService<OutboxPublisherWorkerService>();

        // Background Service thu thap metric dinh ky (moi 30 giay mot lan, FR-06, NFR-07)
        builder.Services.AddHostedService<MetricWorkerService>();

        // Background Service quét và nhả hold hết hạn (WorkerUnHold, mỗi 1 giây)
        builder.Services.AddHostedService<WorkerUnHoldService>();

        var host = builder.Build();

        // Chay MetricEndpointService (Kestrel expose /api/v1/metrics cho Prometheus scrape)
        var serviceProvider = host.Services;
        var logger = serviceProvider.GetRequiredService<ILogger<MetricEndpointService>>();
        var metricEndpointService = new MetricEndpointService(serviceProvider, logger);
        _ = metricEndpointService.StartAsync(default);

        host.Run();
    }
}
