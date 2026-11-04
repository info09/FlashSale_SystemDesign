using FlashSale.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FlashSale.Api.Middleware;

/// <summary>
/// Middleware kiem tra RateLimit truoc khi xu ly request tiep theo.
/// Neu vuot ngưỡng, tra HTTP 429 ngay lap tuc khong goi xuong dich vu ha duong (FR-01, NFR-04).
/// Thuat ngua chuan theo docs/domain-glossary.md: RateLimit, ApiGateway, Http429 (HTTP 429 Too Many Requests).
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _maxRequests;
    private readonly int _windowSeconds;

    public RateLimitMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;

        // Doc ngưỡng rate limit tu bien moi truong (co the override qua bien moi truong)
        _maxRequests = int.Parse(System.Environment.GetEnvironmentVariable("RATE_LIMIT_RPS") ?? "100");
        _windowSeconds = int.Parse(System.Environment.GetEnvironmentVariable("RATE_LIMIT_WINDOW_SECONDS") ?? "60");
    }

    /// <summary>
    /// Thuc thi middleware: kiem tra RateLimit truoc khi cho phep request tiep tuc.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Lay ClientId tu header X-Client-Id (co the sua lai sau khi co chuan auth JWT/API key)
        var clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        // Tao scope de lay IRateLimitService (scoped service)
        using var scope = _scopeFactory.CreateScope();
        var rateLimitService = scope.ServiceProvider.GetRequiredService<IRateLimitService>();

        // Kiem tra RateLimit qua Application Service
        var result = await rateLimitService.CheckRateLimitAsync(
            clientId,
            _maxRequests,
            _windowSeconds,
            context.RequestAborted);

        if (!result.IsAllowed)
        {
            // Tra HTTP 429 ngay lap tuc, khong goi xuong dich vu ha duong
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                status = "error",
                code = "RATE_LIMIT_EXCEEDED",
                message = $"Ban da gui qua nhieu yeu cau. Vui long thu lai sau {result.ResetAfterSeconds} giay.",
                correlationId = context.Request.Headers["Correlation-Id"].FirstOrDefault() ?? string.Empty
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse), context.RequestAborted);
            return;
        }

        // RateLimit cho phep, tiep tuc xu ly request tiep theo
        await _next(context);
    }
}
