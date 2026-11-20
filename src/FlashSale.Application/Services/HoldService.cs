using FlashSale.Application.Features.Hold.Commands;
using FlashSale.Application.Features.Inventory.Events;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Exceptions;
using FlashSale.Domain.Interfaces;
using FlashSale.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlashSale.Application.Services;

/// <summary>
/// Dịch vụ Hold ở lớp Application.
/// Thuật ngữ chuẩn: Hold, HoldExpiry, Inventory, InformationCustomer, Outbox.
/// </summary>
public class HoldService : IHoldService
{
    private readonly IHoldRepository _holdRepository;
    private readonly IInformationCustomerRepository _informationCustomerRepository;
    private readonly IOutboxService _outboxService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRabbitMqPublisherService _rabbitMqPublisherService;
    private readonly ILogger<HoldService> _logger;

    /// <summary>
    /// Khởi tạo HoldService với các dependency phục vụ tạo/confirm/release hold.
    /// </summary>
    public HoldService(
        IHoldRepository holdRepository,
        IInformationCustomerRepository informationCustomerRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork,
        IRabbitMqPublisherService rabbitMqPublisherService,
        ILogger<HoldService> logger)
    {
        _holdRepository = holdRepository;
        _informationCustomerRepository = informationCustomerRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
        _rabbitMqPublisherService = rabbitMqPublisherService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo hold/reserve inventory trên Redis theo mô hình reserve-on-hold.
    /// </summary>
    public async Task<CreateHoldResult> CreateHoldAsync(CreateHoldCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ProductId))
            throw new ArgumentException("ProductId không được để trống.", nameof(command.ProductId));

        if (command.Quantity <= 0)
            throw new ArgumentException("Quantity phải lớn hơn 0.", nameof(command.Quantity));

        if (string.IsNullOrWhiteSpace(command.ClientId))
            throw new ArgumentException("ClientId không được để trống.", nameof(command.ClientId));

        var result = await _holdRepository.CreateHoldAsync(command.ProductId, command.Quantity, command.ClientId, cancellationToken);

        return result.Status switch
        {
            HoldCreationStatus.Success when result.HoldId.HasValue && result.ExpiresAt.HasValue && result.TtlSeconds > 0
                => new CreateHoldResult(result.HoldId.Value, result.ExpiresAt.Value, result.TtlSeconds),
            HoldCreationStatus.InsufficientInventory
                => throw new InsufficientInventoryException(command.ProductId, command.Quantity, result.AvailableQuantity ?? 0),
            HoldCreationStatus.ActiveHoldAlreadyExists
                => throw new ActiveHoldAlreadyExistsException(command.ClientId, result.ExistingHoldId ?? Guid.Empty),
            _ => throw new HoldInvalidException(Guid.Empty)
        };
    }

    /// <summary>
    /// Confirm hold: consume hold, lưu InformationCustomer và ghi Outbox trong transaction DB.
    /// Sau commit, publish ngay ra pipeline (best-effort) để giảm độ trễ; nếu lỗi sẽ để Outbox worker retry.
    /// </summary>
    public async Task<ConfirmHoldResult> ConfirmHoldAsync(ConfirmHoldCommand command, CancellationToken cancellationToken = default)
    {
        if (command.HoldId == Guid.Empty)
            throw new ArgumentException("HoldId không được để trống.", nameof(command.HoldId));

        if (string.IsNullOrWhiteSpace(command.ClientId))
            throw new ArgumentException("ClientId không được để trống.", nameof(command.ClientId));

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Họ tên không được để trống.", nameof(command.Name));

        if (string.IsNullOrWhiteSpace(command.PhoneNumber))
            throw new ArgumentException("Số điện thoại không được để trống.", nameof(command.PhoneNumber));

        if (string.IsNullOrWhiteSpace(command.Address))
            throw new ArgumentException("Địa chỉ không được để trống.", nameof(command.Address));

        var consumeResult = await _holdRepository.ConsumeHoldAsync(command.HoldId, command.ClientId, cancellationToken);
        if (consumeResult.Status != HoldConsumeStatus.Success || consumeResult.Hold is null)
        {
            throw consumeResult.Status switch
            {
                HoldConsumeStatus.Expired => new HoldExpiredException(command.HoldId),
                HoldConsumeStatus.Consumed => new HoldAlreadyConfirmedException(command.HoldId),
                HoldConsumeStatus.NotOwned => new HoldNotOwnedException(command.HoldId, command.ClientId),
                HoldConsumeStatus.NotFound => new HoldInvalidException(command.HoldId),
                HoldConsumeStatus.Invalid => new HoldInvalidException(command.HoldId),
                _ => new HoldInvalidException(command.HoldId)
            };
        }

        var hold = consumeResult.Hold;
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var occurredOn = DateTimeOffset.UtcNow;
        var name = command.Name.Trim();
        var phoneNumber = command.PhoneNumber.Trim();
        var address = command.Address.Trim();

        var outboxPayload = JsonSerializer.Serialize(new
        {
            holdId = hold.Id,
            orderId,
            correlationId,
            productId = hold.ProductId,
            quantity = hold.Quantity,
            clientId = hold.ClientId,
            name,
            phoneNumber,
            address,
            orderStatus = "Created",
            occurredOn
        });

        Outbox outbox;
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var informationCustomer = new InformationCustomer(Guid.NewGuid(), name, address, phoneNumber, hold.Id);
            await _informationCustomerRepository.AddAsync(informationCustomer, cancellationToken);

            outbox = await _outboxService.AddOutboxAsync(orderId, "HoldConfirmed", outboxPayload, correlationId, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await TryPublishHoldConfirmedAsync(outbox, hold, orderId, correlationId, occurredOn, name, phoneNumber, address, cancellationToken);

        return new ConfirmHoldResult(hold.Id, orderId, correlationId, hold.ProductId, hold.Quantity, hold.ClientId);
    }

    /// <summary>
    /// Release hold (hủy/chủ động nhả slot). Idempotent với các trạng thái đã release/expired.
    /// </summary>
    public async Task<ReleaseHoldResult> ReleaseHoldAsync(ReleaseHoldCommand command, CancellationToken cancellationToken = default)
    {
        if (command.HoldId == Guid.Empty)
            throw new ArgumentException("HoldId không được để trống.", nameof(command.HoldId));

        if (string.IsNullOrWhiteSpace(command.ClientId))
            throw new ArgumentException("ClientId không được để trống.", nameof(command.ClientId));

        var result = await _holdRepository.ReleaseHoldAsync(command.HoldId, command.ClientId, cancellationToken);

        return result.Status switch
        {
            HoldReleaseStatus.Released or HoldReleaseStatus.ExpiredReleased or HoldReleaseStatus.AlreadyReleased
                => new ReleaseHoldResult(true),
            HoldReleaseStatus.NotFound => throw new HoldInvalidException(command.HoldId),
            HoldReleaseStatus.NotOwned => throw new HoldNotOwnedException(command.HoldId, command.ClientId),
            HoldReleaseStatus.Consumed => throw new HoldAlreadyConfirmedException(command.HoldId),
            _ => throw new HoldInvalidException(command.HoldId)
        };
    }

    private async Task TryPublishHoldConfirmedAsync(
        Outbox outbox,
        Hold hold,
        Guid orderId,
        Guid correlationId,
        DateTimeOffset occurredOn,
        string name,
        string phoneNumber,
        string address,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = OrderCreatedMessage.CreateFromHoldConfirmation(
                hold.Id,
                orderId,
                hold.ProductId,
                hold.Quantity,
                hold.ClientId,
                name,
                phoneNumber,
                address,
                correlationId,
                occurredOn);

            // Đảm bảo MessageId/OccurredOn gắn với bản ghi Outbox để truy vết/idempotency nhất quán.
            message = message with { MessageId = outbox.Id, OccurredOn = occurredOn };

            await _rabbitMqPublisherService.PublishOrderCreatedAsync(message, cancellationToken);

            outbox.MarkAsProcessed();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Publish ngay OrderCreated từ Hold {HoldId} thất bại, giữ Outbox {OutboxId} Pending để worker retry.",
                hold.Id, outbox.Id);
        }
    }
}
