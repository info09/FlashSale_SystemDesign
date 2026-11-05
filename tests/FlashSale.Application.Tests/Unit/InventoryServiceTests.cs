using FlashSale.Application.Services;
using FlashSale.Domain.Interfaces;
using Moq;

namespace FlashSale.Application.Tests.Unit;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepositoryMock;
    private readonly InventoryService _inventoryService;

    public InventoryServiceTests()
    {
        _inventoryRepositoryMock = new Mock<IInventoryRepository>();
        _inventoryService = new InventoryService(_inventoryRepositoryMock.Object);
    }

}
