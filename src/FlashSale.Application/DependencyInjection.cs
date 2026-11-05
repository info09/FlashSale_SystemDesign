using FlashSale.Application.Features.Auth.CommandHandlers;
using FlashSale.Application.Features.Auth.Commands;
using FlashSale.Application.Features.Auth.Validators;
using FlashSale.Application.Features.Inventory.Commands;
using FlashSale.Application.Features.Inventory.Validators;
using FlashSale.Application.Features.Order.Commands;
using FlashSale.Application.Features.Order.Validators;
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

        // Đăng ký OrderService
        services.AddScoped<IOrderService, OrderService>();

        // Đăng ký OutboxService (quản lý ghi su kien vao bang Outbox, Transactional Outbox Pattern)
        services.AddScoped<IOutboxService, OutboxService>();

        // Đăng ký OutboxPublisher (xuat phat su kien tu Outbox ra RabbitMQ Queue)
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();

        // Đăng ký Validators
        services.AddSingleton<IValidator<RateLimitCommand>, RateLimitCommandValidator>();
        services.AddSingleton<IValidator<DeductInventoryCommand>, DeductInventoryCommandValidator>();
        services.AddSingleton<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddSingleton<IValidator<RegisterCommand>, RegisterCommandValidator>();
        services.AddSingleton<IValidator<CreateOrderCommand>, CreateOrderCommandValidator>();

        return services;
    }
}
