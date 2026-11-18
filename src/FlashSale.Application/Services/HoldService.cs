using System.Text.Json;
using FlashSale.Application.Features.Hold.Commands;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Exceptions;
using FlashSale.Domain.Interfaces;
using FlashSale.Domain.Models;

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

    public HoldService(
        IHoldRepository holdRepository,
        IInformationCustomerRepository informationCustomerRepository,
        IOutboxService outboxService,
        IUnitOfWork unitOfWork)
    {
        _holdRepository = holdRepository;
        _informationCustomerRepository = informationCustomerRepository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateHoldResult> CreateHoldAsync(CreateHoldCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ProductId))
            throw new ArgumentException("ProductId không được để trống", nameof(command.ProductId));
        if (command.Quantity <= 0)
            throw new ArgumentException("Quantity phải lớn hơn 0", nameof(command.Quantity));
        if (string.IsNullOrWhiteSpace(command.ClientId))
            throw new ArgumentException("ClientId không được để trống", nameof(command.ClientId));

        var result = await _holdRepository.CreateHoldAsync(command.ProductId, command.Quantity, command.ClientId, cancellationToken);

        return result.Status switch
        {
            HoldCreationStatus.Success => new CreateHoldResult(
                result.HoldId!.Value,
                result.ExpiresAt!.Value,
                result.TtlSeconds),
            HoldCreationStatus.InsufficientInventory => throw new InsufficientInventoryException(
                command.ProductId,
                command.Quantity,
                result.AvailableQuantity ?? 0),
            HoldCreationStatus.ActiveHoldAlreadyExists => throw new ActiveHoldAlreadyExistsException(
                command.ClientId,
                result.ExistingHoldId ?? Guid.Empty),
            _ => throw new InvalidOperationException($"Trạng thái tạo hold không hỗ trợ: {result.Status}")
        };
    }

    public async Task<ConfirmHoldResult> ConfirmHoldAsync(ConfirmHoldCommand command, CancellationToken cancellationToken = default)
    {
        if (command.HoldId == Guid.Empty)
            throw new ArgumentException("HoldId không được để trống", nameof(command.HoldId));
        if (string.IsNullOrWhiteSpace(command.ClientId))
            throw new ArgumentException("ClientId không được để trống", nameof(command.ClientId));

        var name = command.Name?.Trim();
        var phoneNumber = command.PhoneNumber?.Trim();
        var address = command.Address?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name không được để trống", nameof(command.Name));
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("PhoneNumber không được để trống", nameof(command.PhoneNumber));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address không được để trống", nameof(command.Address));

        var consumeResult = await _holdRepository.ConsumeHoldAsync(command.HoldId, command.ClientId, cancellationToken);

        switch (consumeResult.Status)
        {
            case HoldConsumeStatus.Success:
                break;
            case HoldConsumeStatus.Expired:
                throw new HoldExpiredException(command.HoldId);
            case HoldConsumeStatus.NotFound:
            case HoldConsumeStatus.Invalid:
                throw new HoldInvalidException(command.HoldId);
            case HoldConsumeStatus.Consumed:
                throw new HoldAlreadyConfirmedException(command.HoldId);
            case HoldConsumeStatus.NotOwned:
                throw new HoldNotOwnedException(command.HoldId, command.ClientId);
            default:
                throw new InvalidOperationException($"Trạng thái consume hold không hỗ trợ: {consumeResult.Status}");
        }

        var hold = consumeResult.Hold ?? throw new HoldInvalidException(command.HoldId);
        var orderId = Guid.NewGuid();
        var correlationId = orderId;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var informationCustomer = new InformationCustomer(
                Guid.NewGuid(),
                name,
                address,
                phoneNumber,
                hold.Id);
            await _informationCustomerRepository.AddAsync(informationCustomer, cancellationToken);

            var payload = JsonSerializer.Serialize(new
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
                occurredOn = DateTimeOffset.UtcNow
            });

            await _outboxService.AddOutboxAsync(orderId, "HoldConfirmed", payload, correlationId, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new ConfirmHoldResult(
            hold.Id,
            orderId,
            correlationId,
            hold.ProductId,
            hold.Quantity,
            hold.ClientId);
    }

    public async Task<ReleaseHoldResult> ReleaseHoldAsync(ReleaseHoldCommand command, CancellationToken cancellationToken = default)
    {
        if (command.HoldId == Guid.Empty)
            throw new ArgumentException("HoldId không được để trống", nameof(command.HoldId));
        if (string.IsNullOrWhiteSpace(command.ClientId))
            throw new ArgumentException("ClientId không được để trống", nameof(command.ClientId));

        var result = await _holdRepository.ReleaseHoldAsync(command.HoldId, command.ClientId, cancellationToken);

        return result.Status switch
        {
            HoldReleaseStatus.Released => new ReleaseHoldResult(true),
            HoldReleaseStatus.AlreadyReleased => new ReleaseHoldResult(true),
            HoldReleaseStatus.ExpiredReleased => new ReleaseHoldResult(true),
            HoldReleaseStatus.NotFound => throw new HoldInvalidException(command.HoldId),
            HoldReleaseStatus.Invalid => throw new HoldInvalidException(command.HoldId),
            HoldReleaseStatus.Consumed => throw new HoldAlreadyConfirmedException(command.HoldId),
            _ => throw new InvalidOperationException($"Trạng thái release hold không hỗ trợ: {result.Status}")
        };
    }
}
