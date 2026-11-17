using System.Text.Json;
using FlashSale.Api.Controllers;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlashSale.Api.Tests.Unit;

public class HealthControllerTests
{
    private readonly Mock<IHealthCheckService> _healthCheckServiceMock;
    private readonly Mock<ILogger<HealthController>> _loggerMock;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _healthCheckServiceMock = new Mock<IHealthCheckService>();
        _loggerMock = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_healthCheckServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetHealth_WhenAllServicesHealthy_ShouldReturn200Ok()
    {
        // Arrange
        var healthResult = new HealthResult
        {
            Status = "Healthy",
            Checks = new Dictionary<string, object>
            {
                ["PostgreSQLPrimary"] = "Healthy",
                ["Redis"] = "Healthy",
                ["RabbitMQ"] = "Healthy"
            }
        };

        _healthCheckServiceMock
            .Setup(x => x.CheckAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthResult);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        doc.RootElement.GetProperty("checks").GetProperty("PostgreSQLPrimary").GetString().Should().Be("Healthy");
        doc.RootElement.GetProperty("checks").GetProperty("Redis").GetString().Should().Be("Healthy");
        doc.RootElement.GetProperty("checks").GetProperty("RabbitMQ").GetString().Should().Be("Healthy");

        _healthCheckServiceMock.Verify(x => x.CheckAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHealth_WhenAnyServiceUnhealthy_ShouldReturn503ServiceUnavailable()
    {
        // Arrange
        var healthResult = new HealthResult
        {
            Status = "Unhealthy",
            Checks = new Dictionary<string, object>
            {
                ["PostgreSQLPrimary"] = "Healthy",
                ["Redis"] = "Unhealthy",
                ["RabbitMQ"] = "Healthy"
            }
        };

        _healthCheckServiceMock
            .Setup(x => x.CheckAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthResult);

        // Act
        var result = await _controller.GetHealth(CancellationToken.None);

        // Assert
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        var json = JsonSerializer.Serialize(objectResult.Value);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
        doc.RootElement.GetProperty("checks").GetProperty("Redis").GetString().Should().Be("Unhealthy");

        _healthCheckServiceMock.Verify(x => x.CheckAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
