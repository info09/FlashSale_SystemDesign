using FluentAssertions;
using FlashSale.Application.Commands;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Exceptions;
using FlashSale.Domain.Interfaces;
using FluentAssertions.Execution;
using Moq;
using Xunit;

namespace FlashSale.Application.Tests.Unit;

public class RateLimitServiceTests
{
    private readonly Mock<IRateLimitRepository> _rateLimitRepositoryMock;
    private readonly RateLimitService _rateLimitService;

    public RateLimitServiceTests()
    {
        _rateLimitRepositoryMock = new Mock<IRateLimitRepository>();
        _rateLimitService = new RateLimitService(_rateLimitRepositoryMock.Object);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WhenAllowed_ShouldReturnIsAllowedTrue()
    {
        // Arrange
        var clientId = "client-1";
        var maxRequests = 100;
        var windowSeconds = 60;
        _rateLimitRepositoryMock
            .Setup(x => x.CheckAndConsumeAsync(clientId, maxRequests, windowSeconds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RateLimit.Allowed(clientId, 99, windowSeconds));

        // Act
        var result = await _rateLimitService.CheckRateLimitAsync(clientId, maxRequests, windowSeconds);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.RemainingRequests.Should().Be(99);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WhenRejected_ShouldThrowRateLimitExceededException()
    {
        // Arrange
        var clientId = "client-1";
        var maxRequests = 100;
        var windowSeconds = 60;
        _rateLimitRepositoryMock
            .Setup(x => x.CheckAndConsumeAsync(clientId, maxRequests, windowSeconds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RateLimit.Rejected(clientId, 0, windowSeconds));

        // Act
        var act = () => _rateLimitService.CheckRateLimitAsync(clientId, maxRequests, windowSeconds);

        // Assert
        (await act.Should().ThrowAsync<RateLimitExceededException>())
            .WithMessage("*Rate limit exceeded*");
    }

    [Fact]
    public async Task CheckRateLimitAsync_WhenClientIdNullOrEmpty_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _rateLimitService.CheckRateLimitAsync(string.Empty, 100, 60);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("*ClientId không được để trống*");
    }

    [Fact]
    public async Task CheckRateLimitAsync_WhenMaxRequestsZero_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _rateLimitService.CheckRateLimitAsync("client-1", 0, 60);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("*MaxRequests phải lớn hơn 0*");
    }

    [Fact]
    public async Task CheckRateLimitAsync_WhenWindowSecondsZero_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _rateLimitService.CheckRateLimitAsync("client-1", 100, 0);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("*WindowSeconds phải lớn hơn 0*");
    }
}
