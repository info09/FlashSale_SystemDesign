using FluentAssertions;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Unit;

public class HealthCheckServiceTests
{
    private readonly Mock<IDatabaseConnectionChecker> _databaseConnectionCheckerMock;
    private readonly Mock<IDatabase> _redisDatabaseMock;
    private readonly Mock<RabbitMQ.Client.IConnection> _rabbitMqConnectionMock;
    private readonly Mock<ILogger<HealthCheckService>> _loggerMock;
    private readonly HealthCheckService _service;

    public HealthCheckServiceTests()
    {
        _databaseConnectionCheckerMock = new Mock<IDatabaseConnectionChecker>();
        _redisDatabaseMock = new Mock<IDatabase>();
        _rabbitMqConnectionMock = new Mock<RabbitMQ.Client.IConnection>();
        _loggerMock = new Mock<ILogger<HealthCheckService>>();

        _service = new HealthCheckService(
            _databaseConnectionCheckerMock.Object,
            _redisDatabaseMock.Object,
            _rabbitMqConnectionMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CheckAllAsync_WhenAllServicesHealthy_ShouldReturnHealthy()
    {
        // Arrange
        _databaseConnectionCheckerMock
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _redisDatabaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(5));

        _rabbitMqConnectionMock
            .SetupGet(x => x.IsOpen)
            .Returns(true);

        // Act
        var result = await _service.CheckAllAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be("Healthy");
        result.IsHealthy.Should().BeTrue();
        result.Checks["PostgreSQLPrimary"].Should().Be("Healthy");
        result.Checks["Redis"].Should().Be("Healthy");
        result.Checks["RabbitMQ"].Should().Be("Healthy");
    }

    [Fact]
    public async Task CheckAllAsync_WhenPostgreSQLUnhealthy_ShouldReturnUnhealthy()
    {
        // Arrange
        _databaseConnectionCheckerMock
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _redisDatabaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(5));

        _rabbitMqConnectionMock
            .SetupGet(x => x.IsOpen)
            .Returns(true);

        // Act
        var result = await _service.CheckAllAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be("Unhealthy");
        result.IsHealthy.Should().BeFalse();
        result.Checks["PostgreSQLPrimary"].Should().Be("Unhealthy");
        result.Checks["Redis"].Should().Be("Healthy");
        result.Checks["RabbitMQ"].Should().Be("Healthy");
    }

    [Fact]
    public async Task CheckAllAsync_WhenRedisUnhealthy_ShouldReturnUnhealthy()
    {
        // Arrange
        _databaseConnectionCheckerMock
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _redisDatabaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new System.Exception("Redis connection failed"));

        _rabbitMqConnectionMock
            .SetupGet(x => x.IsOpen)
            .Returns(true);

        // Act
        var result = await _service.CheckAllAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be("Unhealthy");
        result.IsHealthy.Should().BeFalse();
        result.Checks["Redis"].Should().Be("Unhealthy");
    }

    [Fact]
    public async Task CheckAllAsync_WhenRabbitMQUnhealthy_ShouldReturnUnhealthy()
    {
        // Arrange
        _databaseConnectionCheckerMock
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _redisDatabaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(5));

        _rabbitMqConnectionMock
            .SetupGet(x => x.IsOpen)
            .Returns(false);

        // Act
        var result = await _service.CheckAllAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be("Unhealthy");
        result.IsHealthy.Should().BeFalse();
        result.Checks["RabbitMQ"].Should().Be("Unhealthy");
    }
}
