namespace FlashSale.Domain.Entities;

/// <summary>
/// Entity InformationCustomer - thông tin khách hàng nhập khi confirm giữ chỗ.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: InformationCustomer, Hold, Order.
/// </summary>
public class InformationCustomer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string PhoneNumber { get; private set; }
    public Guid HoldId { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }

    private InformationCustomer() { }

    public InformationCustomer(Guid id, string name, string address, string phoneNumber, Guid holdId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name không được để trống", nameof(name));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address không được để trống", nameof(address));
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("PhoneNumber không được để trống", nameof(phoneNumber));
        if (holdId == Guid.Empty)
            throw new ArgumentException("HoldId không được để trống", nameof(holdId));

        Id = id;
        Name = name.Trim();
        Address = address.Trim();
        PhoneNumber = phoneNumber.Trim();
        HoldId = holdId;
        CreatedOn = DateTimeOffset.UtcNow;
    }
}
