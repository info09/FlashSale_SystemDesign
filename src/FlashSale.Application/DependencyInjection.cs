using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlashSale.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Đăng ký Application Service
        services.AddScoped<IRateLimitService, RateLimitService>();
        return services;
    }
}
