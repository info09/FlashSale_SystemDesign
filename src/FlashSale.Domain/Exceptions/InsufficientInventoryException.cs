namespace FlashSale.Domain.Exceptions;

/// <summary>
/// Ngoại lệ ném khi không đủ Inventory để trừ (OverSelling).
/// Controller sẽ bắt ngoại lệ này và trả HTTP 400 Bad Request cho client (FR-02).
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory, OverSelling, InsufficientInventoryException.
/// Không được gọi là: OutOfStockException, QuotaExceededException, StockException.
/// </summary>
public class InsufficientInventoryException : Exception
{
    public string ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientInventoryException(string productId, int requestedQuantity, int availableQuantity)
        : base($"OverSelling: Sản phẩm '{productId}' không đủ tồn kho. Yêu cầu: {requestedQuantity}, Còn lại: {availableQuantity}.")
    {
        ProductId = productId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}
