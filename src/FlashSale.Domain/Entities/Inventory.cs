namespace FlashSale.Domain.Entities;

/// <summary>
/// Entity Inventory (Tồn kho) - số lượng tồn kho của một Product trong Redis RAM.
/// Được trừ atomic qua Lua Script (AtomicInventoryOffloading) để chống OverSelling.
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory, OverSelling, Product, AtomicInventoryOffloading.
/// Không được gọi là: Stock, Tồn kho, Kho hàng, Quantity, Exist_quantity, Available_quantity.
/// </summary>
public class Inventory
{
    public string ProductId { get; private set; }
    public int Quantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => Quantity - ReservedQuantity;

    private Inventory() { }

    public Inventory(string productId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("ProductId không được để trống", nameof(productId));
        if (quantity < 0)
            throw new ArgumentException("Quantity không được âm", nameof(quantity));

        ProductId = productId;
        Quantity = quantity;
        ReservedQuantity = 0;
    }

    /// <summary>
    /// Đặt lại số lượng tồn kho (dùng khi khởi tạo hoặc cập nhật kho).
    /// </summary>
    public void SetQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity không được âm", nameof(quantity));
        Quantity = quantity;
    }

    /// <summary>
    /// Đặt số lượng reserved (đã đặt hàng nhưng chưa thanh toán).
    /// </summary>
    public void SetReservedQuantity(int reservedQuantity)
    {
        if (reservedQuantity < 0)
            throw new ArgumentException("ReservedQuantity không được âm", nameof(reservedQuantity));
        if (reservedQuantity > Quantity)
            throw new ArgumentException("ReservedQuantity không được lớn hơn Quantity", nameof(reservedQuantity));
        ReservedQuantity = reservedQuantity;
    }

    /// <summary>
    /// Kiểm tra có đủ available quantity để trừ không.
    /// </summary>
    public bool HasAvailableQuantity(int quantityToDeduct)
    {
        return quantityToDeduct > 0 && AvailableQuantity >= quantityToDeduct;
    }
}
