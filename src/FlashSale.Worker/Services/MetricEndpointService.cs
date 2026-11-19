using FlashSale.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace FlashSale.Worker.Services;

/// <summary>
/// Endpoint HTTP expose metric (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining)
/// de Prometheus scrape tu Worker. Chay Kestrel tren port 8080.
/// Thuat ngua chuan theo docs/domain-glossary.md: MetricEndpointService, Metric, Prometheus, AsyncWorker.
/// </summary>
public class MetricEndpointService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricEndpointService> _logger;
    private WebApplication? _app;

    public MetricEndpointService(IServiceProvider serviceProvider, ILogger<MetricEndpointService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Bat dau Kestrel server expose /api/v1/metrics.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel();
        //builder.WebHost.UseUrls("http://+:8080");

        // Dung chung IServiceProvider tu Worker host de lay IMetricService (da dang ky qua AddApplication)
        builder.Services.AddSingleton(_serviceProvider);

        var app = builder.Build();

        app.MapGet("/api/v1/metrics", async (HttpContext context) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var metricService = scope.ServiceProvider.GetRequiredService<IMetricService>();
            var prometheusText = await metricService.ExportPrometheusFormatAsync();
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(prometheusText);
        });

        app.MapGet("/api/v1/metrics/{name}", async (string name, HttpContext context) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var metricService = scope.ServiceProvider.GetRequiredService<IMetricService>();
            var metric = await metricService.GetMetricAsync(name);
            if (metric == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("Metric not found");
                return;
            }
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync($"{{\"name\":\"{metric.Name}\",\"value\":{metric.Value},\"unit\":\"{metric.Unit}\"}}");
        });

        _app = app;
        _logger.LogInformation("MetricEndpointService is starting on port 8080");
        return _app.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Dung Kestrel server.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MetricEndpointService is stopping");
        return _app?.StopAsync(cancellationToken) ?? Task.CompletedTask;
    }
}
