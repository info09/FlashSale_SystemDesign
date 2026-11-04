namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao diện định nghĩa thao tác truy cập Inventory trong Redis.
/// Ở lớp Domain chỉ định nghĩa契約 (contract), chưa viết logic thực thi.
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory, IInventoryRepository, AtomicInventoryOffloading, LuaScript.
/// Không được gọi là: IStockRepository, IStockService.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Lấy số lượng tồn kho hiện tại của một Product từ Redis.
    /// </summary>
    /// <param name="productId">Mã định danh sản phẩm.</param>
    /// <param name="cancellationToken">Token hủy bỏ thao tác bất đồng bộ.</param>
    /// <returns>Số lượng tồn kho hiện tại, hoặc null nếu sản phẩm chưa có trong Redis.</returns>
    Task<int?> GetInventoryAsync(string productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trừ kho atomic qua Redis Lua Script (AtomicInventoryOffloading).
    /// Trả về số lượng còn lại sau khi trừ, hoặc null nếu không đủ kho.
    /// </summary>
    /// <param name="productId">Mã định danh sản phẩm.</param>
    /// <param name="quantityToDeduct">Số lượng cần trừ.</param>
    /// <param name="cancellationToken">Token hủy bỏ thao tác bất đồng bộ.</param>
    /// <returns>Số lượng còn lại sau khi trừ, hoặc null nếu không đủ kho (OverSelling).</returns>
    Task<int?> DeductInventoryAsync(string productId, int quantityToDeduct, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đặt số lượng tồn kho ban đầu cho một Product (dùng khi khởi tạo kho).
    /// </summary>
    /// <param name="productId">Mã định danh sản phẩm.</param>
    /// <param name="quantity">Số lượng tồn kho ban đầu.</param>
    /// <param name="cancellationToken">Token hủy bỏ thao tác bất đồng bộ.</param>
    Task SetInventoryAsync(string productId, int quantity, CancellationToken cancellationToken = default);
}
