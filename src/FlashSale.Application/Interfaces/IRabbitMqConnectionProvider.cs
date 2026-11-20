using RabbitMQ.Client;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Cung cấp kết nối RabbitMQ theo kiểu lazy (không kết nối khi khởi động ứng dụng).
/// Dùng để giảm phụ thuộc broker lúc startup và cho phép retry khi broker chưa sẵn sàng.
/// </summary>
public interface IRabbitMqConnectionProvider : IDisposable
{
    /// <summary>
    /// Lấy kết nối RabbitMQ đang mở (kết nối lazy nếu cần).
    /// </summary>
    /// <exception cref="Exception">Ném lỗi nếu không thể kết nối tới broker.</exception>
    IConnection GetConnection();

    /// <summary>
    /// Thử lấy kết nối RabbitMQ. Trả về <c>false</c> nếu không thể kết nối.
    /// </summary>
    bool TryGetConnection(out IConnection? connection);
}
