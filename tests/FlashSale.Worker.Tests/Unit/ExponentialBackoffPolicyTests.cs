using FluentAssertions;
using FlashSale.Application.Services;
using Xunit;

namespace FlashSale.Worker.Tests.Unit;

public class ExponentialBackoffPolicyTests
{
    [Fact]
    public void Constructor_WithDefaultValues_ShouldSetMaxRetryCount3AndBaseDelay1Second()
    {
        // Arrange & Act
        var policy = new ExponentialBackoffPolicy();

        // Assert
        policy.MaxRetryCount.Should().Be(3);
        policy.CanRetry(0).Should().BeTrue();
        policy.CanRetry(1).Should().BeTrue();
        policy.CanRetry(2).Should().BeTrue();
        policy.CanRetry(3).Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithCustomMaxRetryCount_ShouldUseCustomValue()
    {
        // Arrange & Act
        var policy = new ExponentialBackoffPolicy(maxRetryCount: 5);

        // Assert
        policy.MaxRetryCount.Should().Be(5);
        policy.CanRetry(4).Should().BeTrue();
        policy.CanRetry(5).Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithInvalidMaxRetryCount_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => new ExponentialBackoffPolicy(maxRetryCount: 0);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetDelay_WithRetryCount0_ShouldReturnBaseDelay()
    {
        // Arrange
        var policy = new ExponentialBackoffPolicy(maxRetryCount: 3, baseDelay: TimeSpan.FromSeconds(1));

        // Act
        var delay = policy.GetDelay(0);

        // Assert
        delay.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetDelay_WithRetryCount1_ShouldReturnDoubleBaseDelay()
    {
        // Arrange
        var policy = new ExponentialBackoffPolicy(maxRetryCount: 3, baseDelay: TimeSpan.FromSeconds(1));

        // Act
        var delay = policy.GetDelay(1);

        // Assert
        delay.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void GetDelay_WithRetryCount2_ShouldReturnQuadrupleBaseDelay()
    {
        // Arrange
        var policy = new ExponentialBackoffPolicy(maxRetryCount: 3, baseDelay: TimeSpan.FromSeconds(1));

        // Act
        var delay = policy.GetDelay(2);

        // Assert
        delay.Should().Be(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void GetDelay_WithNegativeRetryCount_ShouldThrowArgumentException()
    {
        // Arrange
        var policy = new ExponentialBackoffPolicy();

        // Act & Assert
        var act = () => policy.GetDelay(-1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CanRetry_WithRetryCountBelowMax_ShouldReturnTrue()
    {
        // Arrange
        var policy = new ExponentialBackoffPolicy(maxRetryCount: 3);

        // Act & Assert
        policy.CanRetry(0).Should().BeTrue();
        policy.CanRetry(1).Should().BeTrue();
        policy.CanRetry(2).Should().BeTrue();
    }

    [Fact]
    public void CanRetry_WithRetryCountEqualMax_ShouldReturnFalse()
    {
        // Arrange
        var policy = new ExponentialBackoffPolicy(maxRetryCount: 3);

        // Act & Assert
        policy.CanRetry(3).Should().BeFalse();
    }

    [Fact]
    public void CanRetry_WithRetryCountAboveMax_ShouldReturnFalse()
    {
        // Arrange
        var policy = new ExponentialBackoffPolicy(maxRetryCount: 3);

        // Act & Assert
        policy.CanRetry(4).Should().BeFalse();
        policy.CanRetry(10).Should().BeFalse();
    }
}
