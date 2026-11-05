namespace FlashSale.Domain.Entities;

/// <summary>
/// Entity Don hang (Order) trong he thong Flash Sale.
/// Don hang duoc tao sau khi tru kho thanh cong tren Redis, ghi vao PostgreSQL Primary qua Async Worker.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Order.
/// Khong duoc goi la: Don, Don hang, OrderItem, Purchase, Transaction, Sale, HoaDon.
/// </summary>
public class Order
{
    public Guid Id { get; private set; }
    public string ProductId { get; private set; }
    public int Quantity { get; private set; }
    public string ClientId { get; private set; }
    public string OrderStatus { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }

    private Order() { }

    public Order(string productId, int quantity, string clientId)
        : this(Guid.NewGuid(), productId, quantity, clientId)
    {
    }

    /// <summary>
    /// Khoi tao Order voi Id chinh xac (dung khi load tu kho du lieu/PostgreSQLPrimary).
    /// </summary>
    public Order(Guid id, string productId, int quantity, string clientId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("ProductId không được để trống", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity phải lớn hơn 0", nameof(quantity));
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId không được để trống", nameof(clientId));

        Id = id;
        ProductId = productId;
        Quantity = quantity;
        ClientId = clientId;
        OrderStatus = "Created";
        CreatedOn = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cap nhat trang thai don hang.
    /// </summary>
    public void UpdateStatus(string orderStatus)
    {
        if (string.IsNullOrWhiteSpace(orderStatus))
            throw new ArgumentException("OrderStatus không được để trống", nameof(orderStatus));

        OrderStatus = orderStatus;
    }
}
