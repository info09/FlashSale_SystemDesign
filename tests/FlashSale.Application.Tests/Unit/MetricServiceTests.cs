using FluentAssertions;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FlashSale.Application.Tests.Unit;

public class MetricServiceTests
{
    private readonly Mock<IMetricCollector> _metricCollectorMock;
    private readonly MetricService _service;

    public MetricServiceTests()
    {
        _metricCollectorMock = new Mock<IMetricCollector>();
        _service = new MetricService(_metricCollectorMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<MetricService>>().Object);
    }

    [Fact]
    public async Task GetAllMetricsAsync_ShouldReturnAllDefaultMetrics()
    {
        // Arrange
        _metricCollectorMock
            .Setup(x => x.GetInventoryRemainingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);
        _metricCollectorMock
            .Setup(x => x.GetQueueDepthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _metricCollectorMock
            .Setup(x => x.GetDlqSizeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var metrics = await _service.GetAllMetricsAsync();

        // Assert
        metrics.Should().HaveCount(5);
        metrics.Should().Contain(m => m.Name == "P99Latency");
        metrics.Should().Contain(m => m.Name == "ErrorRate");
        metrics.Should().Contain(m => m.Name == "QueueDepth" && m.Value == 5);
        metrics.Should().Contain(m => m.Name == "DlqSize" && m.Value == 2);
        metrics.Should().Contain(m => m.Name == "InventoryRemaining" && m.Value == 100);
    }

    [Fact]
    public async Task GetMetricAsync_WhenMetricExists_ShouldReturnMetric()
    {
        // Arrange
        _metricCollectorMock
            .Setup(x => x.GetInventoryRemainingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        // Act
        var metric = await _service.GetMetricAsync("InventoryRemaining");

        // Assert
        metric.Should().NotBeNull();
        metric!.Name.Should().Be("InventoryRemaining");
        metric.Value.Should().Be(50);
        metric.Unit.Should().Be("count");
    }

    [Fact]
    public async Task GetMetricAsync_WhenMetricNotExists_ShouldReturnNull()
    {
        // Arrange
        _metricCollectorMock
            .Setup(x => x.GetInventoryRemainingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var metric = await _service.GetMetricAsync("NonExistentMetric");

        // Assert
        metric.Should().BeNull();
    }

    [Fact]
    public async Task SetMetricAsync_ShouldUpdateMetricValue()
    {
        // Arrange & Act
        await _service.SetMetricAsync("P99Latency", 15.5, "ms");

        // Assert
        var metric = await _service.GetMetricAsync("P99Latency");
        metric.Should().NotBeNull();
        metric!.Value.Should().Be(15.5);
        metric.Unit.Should().Be("ms");
    }

    [Fact]
    public async Task ExportPrometheusFormatAsync_ShouldReturnValidPrometheusFormat()
    {
        // Arrange
        await _service.SetMetricAsync("P99Latency", 18.0, "ms");
        await _service.SetMetricAsync("ErrorRate", 0.5, "percent");

        // Act
        var result = await _service.ExportPrometheusFormatAsync();

        // Assert
        result.Should().Contain("# HELP flash_sale_p99latency");
        result.Should().Contain("# TYPE flash_sale_p99latency gauge");
        result.Should().Contain("flash_sale_p99latency 18");
        result.Should().Contain("# HELP flash_sale_errorrate");
        result.Should().Contain("flash_sale_errorrate 0.5");
    }
}
