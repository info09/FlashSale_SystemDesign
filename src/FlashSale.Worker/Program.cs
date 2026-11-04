using Microsoft.Extensions.Hosting;
using FlashSale.Worker;

// Đọc cấu hình từ environment variable (không hardcode secret)
var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
