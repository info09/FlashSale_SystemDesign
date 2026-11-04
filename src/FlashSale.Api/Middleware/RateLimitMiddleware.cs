using FlashSale.Application.Interfaces;

namespace FlashSale.Api.Middleware;

/// <summary>
/// Middleware kiem tra RateLimit truoc khi xu ly request tiep theo.
/// Neu vuot ngưỡng, tra HTTP 429 ngay lap tuc khong goi xuong dich vu ha duong (FR-01, NFR-04).
/// Thuat ngua chuan theo docs/domain-glossary.md: RateLimit, ApiGateway, Http429 (HTTP 429 Too Many Requests).
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitService _rateLimitService;
    private readonly int _maxRequests;
    private readonly int _windowSeconds;

    public RateLimitMiddleware(
        RequestDelegate next,
        IRateLimitService rateLimitService,
        IConfiguration configuration)
    {
        _next = next;
        _rateLimitService = rateLimitService;

        // Doc ngưỡng rate limit tu cau hinh (co the override qua bien moi truong)
        _maxRequests = int.Parse(configuration["RATE_LIMIT_RPS"] ?? "100");
        _windowSeconds = int.Parse(configuration["RATE_LIMIT_WINDOW_SECONDS"] ?? "60");
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

        // Kiem tra RateLimit qua Application Service
        var result = await _rateLimitService.CheckRateLimitAsync(
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
