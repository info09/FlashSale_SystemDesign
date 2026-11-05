using FlashSale.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

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
    private readonly IConfiguration _configuration;
    private readonly int _maxRequests;
    private readonly int _windowSeconds;

    public RateLimitMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _configuration = configuration;

        // Doc ngưỡng rate limit tu cau hinh (co the override qua bien moi truong hoac AddInMemoryCollection trong test)
        var rateLimitRps = _configuration["RATE_LIMIT_RPS"];
        _maxRequests = string.IsNullOrWhiteSpace(rateLimitRps) ? 100 : int.Parse(rateLimitRps);

        var windowSeconds = _configuration["RATE_LIMIT_WINDOW_SECONDS"];
        _windowSeconds = string.IsNullOrWhiteSpace(windowSeconds) ? 60 : int.Parse(windowSeconds);
    }

    /// <summary>
    /// Thuc thi middleware: kiem tra RateLimit truoc khi cho phep request tiep tuc.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Lay ClientId tu claim NameIdentifier cua user da xac thuc (JWT Bearer).
        // Neu chua xac thuc, fallback ve header X-Client-Id, sau do IP.
        var clientId = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;

        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault();
        }

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
