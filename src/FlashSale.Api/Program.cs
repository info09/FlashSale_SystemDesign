using FlashSale.Api.Middleware;
using FlashSale.Application;
using FlashSale.Infrastructure;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Đọc cấu hình từ environment variable (không hardcode secret)
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddControllers();

// Xac thuc JWT Bearer (FR-01, NFR-04)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT_ISSUER"],
            ValidAudience = builder.Configuration["JWT_AUDIENCE"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JWT_SECRET"] ?? "FlashSaleSecretKey123456789012345678901234567890")),
            ClockSkew = TimeSpan.Zero
        };
    });

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

// Xac thuc va phan quyen (FR-01, NFR-04)
app.UseAuthentication();
app.UseAuthorization();

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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // Chạy di cư cơ sở dữ liệu (tùy chọn, dành cho môi trường sản xuất)
        var dbContext = services.GetRequiredService<PostgreSQLPrimaryContext>();
        await dbContext.Database.MigrateAsync();

        // Seed người dùng admin và các vai trò
        //await AdminSeeder.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi khi khởi tạo cơ sở dữ liệu");
    }
}

app.Run();
