using FlashSale.Application.Features.Auth.CommandHandlers;
using FlashSale.Application.Features.Auth.Commands;
using FlashSale.Application.Features.Auth.Validators;
using FlashSale.Application.Features.Inventory.Commands;
using FlashSale.Application.Features.Inventory.Validators;
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

        // Đăng ký Auth Commands & Validators
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegisterCommandHandler).Assembly));

        // Đăng ký Validators
        services.AddSingleton<IValidator<RateLimitCommand>, RateLimitCommandValidator>();
        services.AddSingleton<IValidator<DeductInventoryCommand>, DeductInventoryCommandValidator>();
        services.AddSingleton<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddSingleton<IValidator<RegisterCommand>, RegisterCommandValidator>();

        return services;
    }
}
