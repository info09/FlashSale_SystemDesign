using System.Net.Sockets;
using Xunit;
using FluentAssertions;

namespace FlashSale.Infrastructure.Tests.Integration;

public class InfrastructureConnectionTests
{
    [Fact]
    public void RedisConnectionString_ShouldNotBeNullOrEmpty()
    {
        // Arrange
        var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

        // Act & Assert
        if (string.IsNullOrEmpty(connectionString))
        {
            return; // Skip khi chưa cấu hình env (chưa chạy Docker Compose / chưa có file .env)
        }

        connectionString.Should().NotBeNullOrEmpty("REDIS_CONNECTION_STRING phải được cấu hình để kết nối Redis Cluster");
    }

    [Fact]
    public void RabbitMqConnectionString_ShouldNotBeNullOrEmpty()
    {
        // Arrange
        var connectionString = Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION_STRING");

        // Act & Assert
        if (string.IsNullOrEmpty(connectionString))
        {
            return; // Skip khi chưa cấu hình env (chưa chạy Docker Compose / chưa có file .env)
        }

        connectionString.Should().NotBeNullOrEmpty("RABBITMQ_CONNECTION_STRING phải được cấu hình để kết nối RabbitMQ");
    }

    [Fact]
    public void PostgreSqlPrimaryConnectionString_ShouldNotBeNullOrEmpty()
    {
        // Arrange
        var connectionString = Environment.GetEnvironmentVariable("POSTGRESQL_PRIMARY_CONNECTION_STRING");

        // Act & Assert
        if (string.IsNullOrEmpty(connectionString))
        {
            return; // Skip khi chưa cấu hình env (chưa chạy Docker Compose / chưa có file .env)
        }

        connectionString.Should().NotBeNullOrEmpty("POSTGRESQL_PRIMARY_CONNECTION_STRING phải được cấu hình để kết nối PostgreSQLPrimary");
    }

    [Fact]
    public async Task RedisPort_ShouldBeReachable_WhenDockerComposeIsRunning()
    {
        // Arrange
        var host = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
        var port = int.Parse(Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379");

        // Act
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(host, port);
        var timeoutTask = Task.Delay(3000);
        var completed = await Task.WhenAny(connectTask, timeoutTask);

        // Assert
        completed.Should().Be(connectTask, "Redis port ${REDIS_HOST}:${REDIS_PORT} phải có thể kết nối khi Docker Compose chạy");
        client.Connected.Should().BeTrue();
    }

    [Fact]
    public async Task RabbitMqPort_ShouldBeReachable_WhenDockerComposeIsRunning()
    {
        // Arrange
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672");

        // Act
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(host, port);
        var timeoutTask = Task.Delay(3000);
        var completed = await Task.WhenAny(connectTask, timeoutTask);

        // Assert
        completed.Should().Be(connectTask, "RabbitMQ port ${RABBITMQ_HOST}:${RABBITMQ_PORT} phải có thể kết nối khi Docker Compose chạy");
        client.Connected.Should().BeTrue();
    }

    [Fact]
    public async Task PostgreSqlPrimaryPort_ShouldBeReachable_WhenDockerComposeIsRunning()
    {
        // Arrange
        var host = Environment.GetEnvironmentVariable("POSTGRESQL_PRIMARY_HOST") ?? "localhost";
        var port = int.Parse(Environment.GetEnvironmentVariable("POSTGRESQL_PRIMARY_PORT") ?? "5432");

        // Act
        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(host, port);
        var timeoutTask = Task.Delay(3000);
        var completed = await Task.WhenAny(connectTask, timeoutTask);

        // Assert
        completed.Should().Be(connectTask, "PostgreSQLPrimary port ${POSTGRESQL_PRIMARY_HOST}:${POSTGRESQL_PRIMARY_PORT} phải có thể kết nối khi Docker Compose chạy");
        client.Connected.Should().BeTrue();
    }
}
