namespace FlashSale.Domain.Entities;

/// <summary>
/// Entity nguoi dung trong he thong Flash Sale (dung cho ASP.NET Core Identity).
/// Luu thong tin dang nhap: UserName, Email, PasswordHash.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: User.
/// Khong duoc goi la: Customer, Account, KhachHang, NguoiDung.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string UserName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }

    private User() { }

    public User(string userName, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("UserName không được để trống", nameof(userName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email không được để trống", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash không được để trống", nameof(passwordHash));

        Id = Guid.NewGuid();
        UserName = userName;
        Email = email;
        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Cap nhat mat khau da hash (dung khi doi mat khau hoac reset mat khau).
    /// </summary>
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash không được để trống", nameof(passwordHash));
        PasswordHash = passwordHash;
    }
}
