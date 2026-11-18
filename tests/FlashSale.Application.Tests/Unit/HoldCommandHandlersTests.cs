using FlashSale.Application.Features.Hold.CommandHandlers;
using FlashSale.Application.Features.Hold.Commands;
using FlashSale.Application.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace FlashSale.Application.Tests.Unit;

public class HoldCommandHandlersTests
{
    [Fact]
    public async Task CreateHoldCommandHandler_ShouldDelegateToService()
    {
        var service = new Mock<IHoldService>();
        var command = new CreateHoldCommand("p1", 1, "c1");
        var expected = new CreateHoldResult(Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(60), 60);
        service.Setup(x => x.CreateHoldAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new CreateHoldCommandHandler(service.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
        service.Verify(x => x.CreateHoldAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmHoldCommandHandler_ShouldDelegateToService()
    {
        var service = new Mock<IHoldService>();
        var command = new ConfirmHoldCommand(Guid.NewGuid(), "c1", "Name", "0900", "Addr");
        var expected = new ConfirmHoldResult(command.HoldId, Guid.NewGuid(), Guid.NewGuid(), "p1", 1, "c1");
        service.Setup(x => x.ConfirmHoldAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new ConfirmHoldCommandHandler(service.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
        service.Verify(x => x.ConfirmHoldAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReleaseHoldCommandHandler_ShouldDelegateToService()
    {
        var service = new Mock<IHoldService>();
        var command = new ReleaseHoldCommand(Guid.NewGuid(), "c1");
        var expected = new ReleaseHoldResult(true);
        service.Setup(x => x.ReleaseHoldAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new ReleaseHoldCommandHandler(service.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
        service.Verify(x => x.ReleaseHoldAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}
