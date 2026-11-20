using FlashSale.Application.Interfaces;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using StackExchange.Redis;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Unit;

public class HealthCheckServiceTests
{
    private readonly Mock<IDatabaseConnectionChecker> _databaseConnectionCheckerMock;
    private readonly Mock<IDatabase> _redisDatabaseMock;
    private readonly Mock<IRabbitMqConnectionProvider> _rabbitMqConnectionProviderMock;
    private readonly Mock<IConnection> _rabbitMqConnectionMock;
    private readonly Mock<ILogger<HealthCheckService>> _loggerMock;
    private readonly HealthCheckService _service;

    public HealthCheckServiceTests()
    {
        _databaseConnectionCheckerMock = new Mock<IDatabaseConnectionChecker>();
        _redisDatabaseMock = new Mock<IDatabase>();
        _rabbitMqConnectionProviderMock = new Mock<IRabbitMqConnectionProvider>();
        _rabbitMqConnectionMock = new Mock<IConnection>();
        _loggerMock = new Mock<ILogger<HealthCheckService>>();

        _service = new HealthCheckService(
            _databaseConnectionCheckerMock.Object,
            _redisDatabaseMock.Object,
            _rabbitMqConnectionProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CheckAllAsync_WhenAllServicesHealthy_ShouldReturnHealthy()
    {
        _databaseConnectionCheckerMock
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _redisDatabaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(5));

        var connection = _rabbitMqConnectionMock.Object;
        _rabbitMqConnectionMock.SetupGet(x => x.IsOpen).Returns(true);
        _rabbitMqConnectionProviderMock
            .Setup(x => x.TryGetConnection(out connection))
            .Returns(true);

        var result = await _service.CheckAllAsync(CancellationToken.None);

        result.Status.Should().Be("Healthy");
        result.IsHealthy.Should().BeTrue();
        result.Checks["PostgreSQLPrimary"].Should().Be("Healthy");
        result.Checks["Redis"].Should().Be("Healthy");
        result.Checks["RabbitMQ"].Should().Be("Healthy");
    }

    [Fact]
    public async Task CheckAllAsync_WhenPostgreSQLUnhealthy_ShouldReturnUnhealthy()
    {
        _databaseConnectionCheckerMock
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _redisDatabaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(5));

        var connection = _rabbitMqConnectionMock.Object;
        _rabbitMqConnectionMock.SetupGet(x => x.IsOpen).Returns(true);
        _rabbitMqConnectionProviderMock
            .Setup(x => x.TryGetConnection(out connection))
            .Returns(true);

        var result = await _service.CheckAllAsync(CancellationToken.None);

        result.Status.Should().Be("Unhealthy");
        result.IsHealthy.Should().BeFalse();
        result.Checks["PostgreSQLPrimary"].Should().Be("Unhealthy");
        result.Checks["Redis"].Should().Be("Healthy");
        result.Checks["RabbitMQ"].Should().Be("Healthy");
    }

    [Fact]
    public async Task CheckAllAsync_WhenRedisUnhealthy_ShouldReturnUnhealthy()
    {
        _databaseConnectionCheckerMock
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _redisDatabaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new Exception("Redis connection failed"));

        var connection = _rabbitMqConnectionMock.Object;
        _rabbitMqConnectionMock.SetupGet(x => x.IsOpen).Returns(true);
        _rabbitMqConnectionProviderMock
            .Setup(x => x.TryGetConnection(out connection))
            .Returns(true);

        var result = await _service.CheckAllAsync(CancellationToken.None);

        result.Status.Should().Be("Unhealthy");
        result.IsHealthy.Should().BeFalse();
        result.Checks["Redis"].Should().Be("Unhealthy");
    }

    [Fact]
    public async Task CheckAllAsync_WhenRabbitMQCannotConnect_ShouldReturnUnhealthy()
    {
        _databaseConnectionCheckerMock
            .Setup(x => x.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _redisDatabaseMock
            .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(5));

        IConnection? connection = null;
        _rabbitMqConnectionProviderMock
            .Setup(x => x.TryGetConnection(out connection))
            .Returns(false);

        var result = await _service.CheckAllAsync(CancellationToken.None);

        result.Status.Should().Be("Unhealthy");
        result.IsHealthy.Should().BeFalse();
        result.Checks["RabbitMQ"].Should().Be("Unhealthy");
    }
}
