using FlashSale.Api.Middleware;
using FlashSale.Application;
using FlashSale.Infrastructure;

// Đọc cấu hình từ environment variable (không hardcode secret)
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddControllers();
builder.Services.AddSingleton(builder.Configuration);

// OpenAPI (Swagger) + Scalar API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FlashSale API",
        Version = "v1",
        Description = "Distributed Flash-Sale Engine — API Core (RateLimit, AtomicInventoryOffloading, OrderPipeline)"
    });
});

var app = builder.Build();

// NFR-04: Bắt buộc tất cả API endpoint chỉ cho phép gọi qua HTTPS
// Chuyển hướng HTTP sang HTTPS tự động
app.UseHttpsRedirection();

// OpenAPI endpoint (Swagger JSON)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlashSale API v1");
});

// Đăng ký RateLimitMiddleware TRUOC khi map endpoints (FR-01, NFR-04)
app.UseMiddleware<RateLimitMiddleware>();

app.MapControllers();

app.MapGet("/", () => "FlashSale Api is running");
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
