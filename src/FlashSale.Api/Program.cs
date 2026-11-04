using FlashSale.Api.Middleware;
using FlashSale.Application;
using FlashSale.Infrastructure;

// Đọc cấu hình từ environment variable (không hardcode secret)
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

// NFR-04: Bắt buộc tất cả API endpoint chỉ cho phép gọi qua HTTPS
// Chuyển hướng HTTP sang HTTPS tự động
app.UseHttpsRedirection();

// Đăng ký RateLimitMiddleware TRUOC khi map endpoints (FR-01, NFR-04)
app.UseMiddleware<RateLimitMiddleware>();

app.MapGet("/", () => "FlashSale Api is running");
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
