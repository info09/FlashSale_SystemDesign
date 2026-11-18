namespace FlashSale.Domain.Entities;

/// <summary>
/// Trạng thái của Hold.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Hold.
/// </summary>
public enum HoldStatus
{
    Active = 0,
    Consumed = 1,
    Released = 2,
    Expired = 3
}
