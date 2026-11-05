namespace FlashSale.Domain.Entities;

/// <summary>
/// Entity nguoi dung trong he thong Flash Sale (thong tin profile, khong luu PasswordHash).
/// Xac thuc (mat khau, hash) duoc ASP.NET Core Identity xu ly o Infrastructure.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User.
/// Khong duoc goi la: Customer, Account, KhachHang, NguoiDung.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string UserName { get; private set; }
    public string Email { get; private set; }

    private User() { }

    public User(string userName, string email)
        : this(Guid.NewGuid(), userName, email)
    {
    }

    /// <summary>
    /// Khoi tao User voi Id chinh xac (dung khi load tu kho du lieu/PostgreSQLPrimary).
    /// </summary>
    public User(Guid id, string userName, string email)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("UserName không được để trống", nameof(userName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email không được để trống", nameof(email));

        Id = id;
        UserName = userName;
        Email = email;
    }
}
