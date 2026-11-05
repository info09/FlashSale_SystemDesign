using FlashSale.Api.Middleware;
using FlashSale.Application;
using FlashSale.Infrastructure;
using FlashSale.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// Đọc cấu hình từ environment variable (không hardcode secret)
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Cấu hình Serilog (ghi log có cấu trúc, không dùng Console.WriteLine theo Convention 3.1 & 3.10)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddControllers();

// Xac thuc JWT Bearer (FR-01, NFR-04)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        var config = builder.Configuration;
        var jwtSecret = config["JWT_SECRET"] ?? "FlashSaleSecretKey123456789012345678901234567890";
        var jwtIssuer = config["JWT_ISSUER"] ?? "FlashSale.Api";
        var jwtAudience = config["JWT_AUDIENCE"] ?? "FlashSale.Client";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero // Không cho phép thời gian token đã hết hạn bị lệch
        };
        // Log chi loi xac thuc de chuan doan
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.Headers.Append("x-auth-error", context.Exception.Message);
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("JWT token validated for user: {UserName}", context.Principal.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

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

    // Cấu hình JWT Bearer authentication cho Swagger/Scalar
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập JWT Bearer token: Bearer <token>"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// NFR-04: Bắt buộc tất cả API endpoint chỉ cho phép gọi qua HTTPS
// Chuyển hướng HTTP sang HTTPS tự động
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Xac thuc va phan quyen (FR-01, NFR-04) - Phai chay truoc RateLimitMiddleware
// de context.User.Identity duoc populate tu JWT token
app.UseAuthentication();
app.UseAuthorization();

// Bat toan bo exception, tra JSON loi chuan (truyoc khi map endpoints)
app.UseMiddleware<ExceptionHandlerMiddleware>();

// Đăng ký RateLimitMiddleware SAU khi Authentication (FR-01, NFR-04)
// de co the lay ClientId tu Claim NameIdentifier cua user da xac thuc
app.UseMiddleware<RateLimitMiddleware>();

// OpenAPI endpoint (Swagger JSON)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlashSale API v1");
});



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
        await AdminSeeder.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi khi khởi tạo cơ sở dữ liệu");
    }
}

app.Run();
