namespace FlashSale.Domain.Exceptions;

/// <summary>
/// Ngoai le nem khi chuyen message vao DeadLetterQueue that bai (message da vuot qua so lan thu lai toi da).
/// Thuat ngua chuan theo docs/domain-glossary.md: DeadLetterQueue, DeadLetterQueueException (FR-05).
/// Khong duoc goi là: DlqException, FailedMessageException, DeadLetterException.
/// </summary>
public class DeadLetterQueueException : Exception
{
    /// <summary>Ma dinh danh cua message bi chuyen vao DeadLetterQueue.</summary>
    public string MessageId { get; }

    /// <summary>Ten DeadLetterQueue nhan message.</summary>
    public string DeadLetterQueueName { get; }

    public DeadLetterQueueException(string messageId, string deadLetterQueueName)
        : base($"Message '{messageId}' da vuot qua so lan thu lai toi da va da chuyen vao DeadLetterQueue '{deadLetterQueueName}'.")
    {
        MessageId = messageId;
        DeadLetterQueueName = deadLetterQueueName;
    }
}
