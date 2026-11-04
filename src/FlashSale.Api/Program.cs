var builder = WebApplication.CreateBuilder(args);

// Đọc cấu hình từ environment variable (không hardcode secret)
builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

app.MapGet("/", () => "FlashSale Api is running");
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
