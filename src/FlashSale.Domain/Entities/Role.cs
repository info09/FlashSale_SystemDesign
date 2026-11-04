namespace FlashSale.Domain.Entities;

/// <summary>
/// Entity vai trò (Role) cua nguoi dung trong he thong Flash Sale.
/// Dung de phan quyen truy cap API (vi du: FlashSaleShopper, DevOps).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Role.
/// </summary>
public class Role
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    private Role() { }

    public Role(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name không được để trống", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
    }
}
