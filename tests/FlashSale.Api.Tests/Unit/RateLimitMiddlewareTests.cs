using FlashSale.Api.Middleware;
using FlashSale.Application.Features.RateLimit.Commands;
using FlashSale.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace FlashSale.Api.Tests.Unit;

public class RateLimitMiddlewareTests
{
    private readonly Mock<IRateLimitService> _rateLimitServiceMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<RateLimitMiddleware>> _loggerMock;
    private readonly RateLimitMiddleware _middleware;

    public RateLimitMiddlewareTests()
    {
        _rateLimitServiceMock = new Mock<IRateLimitService>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<RateLimitMiddleware>>();

        // Mac dau: 100 requests / 60 giay
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("RATE_LIMIT_RPS", "100"),
                new KeyValuePair<string, string>("RATE_LIMIT_WINDOW_SECONDS", "60"),
            })
            .Build();

        _middleware = new RateLimitMiddleware(
            next: async (context) =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync("OK");
            },
            scopeFactory: _scopeFactoryMock.Object,
            configuration: config);
    }

    [Fact]
    public async Task InvokeAsync_WhenRateLimitAllowed_ShouldCallNextAndReturn200()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Path = "/test"
            },
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Connection.RemoteIpAddress = null;
        context.Request.Headers["X-Client-Id"] = "client-1";
        context.Request.Headers["Correlation-Id"] = "corr-1";

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IRateLimitService)))
            .Returns(_rateLimitServiceMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(scopeMock.Object);

        _rateLimitServiceMock
            .Setup(x => x.CheckRateLimitAsync("client-1", 100, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RateLimitResult(true, 99, 0));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Be("OK");
    }

    [Fact]
    public async Task InvokeAsync_WhenRateLimitExceeded_ShouldReturn429()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Path = "/test"
            },
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Connection.RemoteIpAddress = null;
        context.Request.Headers["X-Client-Id"] = "client-1";
        context.Request.Headers["Correlation-Id"] = "corr-1";

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IRateLimitService)))
            .Returns(_rateLimitServiceMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(scopeMock.Object);

        _rateLimitServiceMock
            .Setup(x => x.CheckRateLimitAsync("client-1", 100, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RateLimitResult(false, 0, 30));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("error");
        doc.RootElement.GetProperty("code").GetString().Should().Be("RATE_LIMIT_EXCEEDED");
        doc.RootElement.GetProperty("message").GetString().Should().Contain("30 giay");
    }

    [Fact]
    public async Task InvokeAsync_WhenNoClientIdHeader_ShouldUseRemoteIpOrUnknown()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Path = "/test"
            },
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Connection.RemoteIpAddress = null;

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IRateLimitService)))
            .Returns(_rateLimitServiceMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(scopeMock.Object);

        _rateLimitServiceMock
            .Setup(x => x.CheckRateLimitAsync("unknown", 100, 60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RateLimitResult(true, 99, 0));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        _rateLimitServiceMock.Verify(x => x.CheckRateLimitAsync(
            "unknown", 100, 60, It.IsAny<CancellationToken>()), Times.Once);
    }
}
