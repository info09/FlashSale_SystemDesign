using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlashSale.Infrastructure.Persistence;

/// <summary>
/// Factory tạo <see cref="PostgreSQLPrimaryContext"/> tại design-time cho EF Core migrations.
/// Dùng khi chạy <c>dotnet ef</c> hoặc <c>Add-Migration</c> mà host ứng dụng không thể khởi tạo DbContext.
/// </summary>
public class PostgreSQLPrimaryContextFactory : IDesignTimeDbContextFactory<PostgreSQLPrimaryContext>
{
    /// <summary>
    /// Tạo DbContext với chuỗi kết nối PostgreSQL phục vụ design-time/migration.
    /// </summary>
    /// <param name="args">Tham số dòng lệnh (không sử dụng).</param>
    /// <returns>Instance <see cref="PostgreSQLPrimaryContext"/> đã cấu hình Npgsql.</returns>
    public PostgreSQLPrimaryContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("POSTGRESQL_PRIMARY_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=flashsale;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<PostgreSQLPrimaryContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PostgreSQLPrimaryContext(optionsBuilder.Options);
    }
}
