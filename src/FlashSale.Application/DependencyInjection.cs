using FlashSale.Application.Features.Inventory.Commands;
using FlashSale.Application.Features.Inventory.Validators;
using FlashSale.Application.Features.RateLimit.CommandHandlers;
using FlashSale.Application.Features.RateLimit.Commands;
using FlashSale.Application.Features.RateLimit.Validators;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FlashSale.Application;

/// <summary>
/// Extension methods de dang ky cac dich vu Application.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Đăng ký RateLimitService
        services.AddScoped<IRateLimitService, RateLimitService>();

        // Đăng ký InventoryService
        services.AddScoped<IInventoryService, InventoryService>();

        // Đăng ký MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RateLimitCommandHandler).Assembly));

        // Đăng ký Validators
        services.AddSingleton<IValidator<RateLimitCommand>, RateLimitCommandValidator>();
        services.AddSingleton<IValidator<DeductInventoryCommand>, DeductInventoryCommandValidator>();

        return services;
    }
}
