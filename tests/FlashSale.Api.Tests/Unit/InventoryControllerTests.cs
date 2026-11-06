using FlashSale.Api.Controllers;
using FlashSale.Application.Features.Inventory.Commands;
using FlashSale.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text.Json;
using Xunit;

namespace FlashSale.Api.Tests.Unit;

public class InventoryControllerTests
{
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly InventoryController _controller;

    public InventoryControllerTests()
    {
        _inventoryServiceMock = new Mock<IInventoryService>();
        _controller = new InventoryController(_inventoryServiceMock.Object);
    }

    [Fact]
    public async Task DeductInventory_WhenSuccess_ShouldReturn200Ok()
    {
        // Arrange
        var request = new DeductInventoryRequest("product-1", 10);
        _inventoryServiceMock
            .Setup(x => x.DeductInventoryAsync(request.ProductId, request.QuantityToDeduct, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeductInventoryResult(true, 90, 10));

        // Act
        var result = await _controller.DeductInventory(request);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var json = JsonSerializer.Serialize(okResult.Value);
        json.Should().Contain("\"status\":\"success\"");
        json.Should().Contain("\"code\":\"INVENTORY_DEDUCTED\"");
        json.Should().Contain("\"deductedQuantity\":10");
        json.Should().Contain("\"remainingQuantity\":90");
    }

    [Fact]
    public async Task DeductInventory_WhenNotEnoughInventory_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new DeductInventoryRequest("product-1", 100);
        _inventoryServiceMock
            .Setup(x => x.DeductInventoryAsync(request.ProductId, request.QuantityToDeduct, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeductInventoryResult(false, 0, 0));

        // Act
        var result = await _controller.DeductInventory(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);

        var json = JsonSerializer.Serialize(badRequestResult.Value);
        json.Should().Contain("\"status\":\"error\"");
        json.Should().Contain("\"code\":\"INSUFFICIENT_INVENTORY\"");
    }

    [Fact]
    public async Task DeductInventory_WhenProductIdEmpty_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new DeductInventoryRequest("", 10);

        // Act
        var result = await _controller.DeductInventory(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task DeductInventory_WhenQuantityToDeductZero_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new DeductInventoryRequest("product-1", 0);

        // Act
        var result = await _controller.DeductInventory(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task DeductInventory_WhenProductIdNull_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new DeductInventoryRequest(null!, 10);

        // Act
        var result = await _controller.DeductInventory(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task AddInventory_WhenSuccess_ShouldReturn200Ok()
    {
        // Arrange
        var request = new AddInventoryRequest("product-1", 100);
        _inventoryServiceMock
            .Setup(x => x.AddInventoryAsync(request.ProductId, request.QuantityToAdd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddInventoryResult(true, 100, 100));

        // Act
        var result = await _controller.AddInventory(request);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult.StatusCode.Should().Be(200);

        var json = JsonSerializer.Serialize(okResult.Value);
        json.Should().Contain("\"status\":\"success\"");
        json.Should().Contain("\"code\":\"INVENTORY_ADDED\"");
        json.Should().Contain("\"addedQuantity\":100");
        json.Should().Contain("\"remainingQuantity\":100");
    }

    [Fact]
    public async Task AddInventory_WhenProductIdEmpty_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new AddInventoryRequest("", 100);

        // Act
        var result = await _controller.AddInventory(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task AddInventory_WhenQuantityToAddZero_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new AddInventoryRequest("product-1", 0);

        // Act
        var result = await _controller.AddInventory(request);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult.StatusCode.Should().Be(400);
    }
}
