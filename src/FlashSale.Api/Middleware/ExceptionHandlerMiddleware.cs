using FlashSale.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlashSale.Api.Middleware;

/// <summary>
/// Middleware bat toan bo exception, tra ve JSON loi theo chuan duoc chot
/// trong docs/conventions.md phan 4.3 va 4.5.
/// Khong swallow exception, luon log co cau truc kèm Correlation-Id.
/// </summary>
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InsufficientInventoryException ex)
        {
            _logger.LogWarning(ex, "Insufficient inventory for product {ProductId}. Requested: {RequestedQuantity}, Available: {AvailableQuantity}.",
                ex.ProductId, ex.RequestedQuantity, ex.AvailableQuantity);
            await WriteErrorResponse(context, StatusCodes.Status400BadRequest, "INSUFFICIENT_INVENTORY", ex.Message);
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning(ex, "Rate limit exceeded for client {ClientId}. Reset after {ResetAfterSeconds} seconds.",
                ex.ClientId, ex.ResetAfterSeconds);
            await WriteErrorResponse(context, StatusCodes.Status429TooManyRequests, "RATE_LIMIT_EXCEEDED", ex.Message);
        }
        catch (ActiveHoldAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "Active hold already exists for client {ClientId}. ExistingHoldId: {ExistingHoldId}", ex.ClientId, ex.ExistingHoldId);
            await WriteErrorResponse(context, StatusCodes.Status409Conflict, "HOLD_ALREADY_EXISTS", ex.Message);
        }
        catch (HoldExpiredException ex)
        {
            _logger.LogWarning(ex, "Hold expired {HoldId}", ex.HoldId);
            await WriteErrorResponse(context, StatusCodes.Status410Gone, "HOLD_EXPIRED", ex.Message);
        }
        catch (HoldNotOwnedException ex)
        {
            _logger.LogWarning(ex, "Hold not owned {HoldId} by client {ClientId}", ex.HoldId, ex.ClientId);
            await WriteErrorResponse(context, StatusCodes.Status403Forbidden, "HOLD_NOT_OWNED", ex.Message);
        }
        catch (HoldConsumedException ex)
        {
            _logger.LogWarning(ex, "Hold consumed {HoldId}", ex.HoldId);
            await WriteErrorResponse(context, StatusCodes.Status409Conflict, "HOLD_CONSUMED", ex.Message);
        }
        catch (HoldAlreadyConfirmedException ex)
        {
            _logger.LogWarning(ex, "Hold already confirmed {HoldId}", ex.HoldId);
            await WriteErrorResponse(context, StatusCodes.Status409Conflict, "HOLD_ALREADY_CONFIRMED", ex.Message);
        }
        catch (HoldInvalidException ex)
        {
            _logger.LogWarning(ex, "Hold invalid {HoldId}", ex.HoldId);
            await WriteErrorResponse(context, StatusCodes.Status409Conflict, "HOLD_INVALID", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business error: {Message}", ex.Message);
            await WriteErrorResponse(context, StatusCodes.Status400BadRequest, "BUSINESS_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError, "INTERNAL_SERVER_ERROR", ex.Message);
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = "error",
            code = code,
            message = message,
            correlationId = context.Request.Headers["Correlation-Id"].FirstOrDefault() ?? string.Empty
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
