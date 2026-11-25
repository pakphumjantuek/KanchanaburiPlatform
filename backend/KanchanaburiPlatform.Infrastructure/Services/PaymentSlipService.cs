using Application.Constants;
using Infrastructure.Services;
using Application.Results;
using Application.DTOs;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace Infrastructure.Services;

public interface IPaymentSlipService
{
    Task<ServiceResult<Order>> UploadAsync(
        Guid orderId,
        UploadPaymentSlipFormDto dto,
        string? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentSlipService(IUnitOfWork unit, IImageUploadService imageUploadService) : IPaymentSlipService
{
    public async Task<ServiceResult<Order>> UploadAsync(
        Guid orderId,
        UploadPaymentSlipFormDto dto,
        string? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var order = await unit.Repository<Order>().GetByIdAsync(orderId);
        if (order == null) return ServiceResult<Order>.Failure(ServiceStatus.NotFound);
        if (order.UserId != userId && !isAdmin) return ServiceResult<Order>.Failure(ServiceStatus.Forbidden);
        if (order.PaymentStatus == PaymentStatuses.Paid) return ServiceResult<Order>.Failure(ServiceStatus.Conflict, "This order has already been paid.");
        if (dto.SlipFile == null) return ServiceResult<Order>.Failure(ServiceStatus.Invalid, "A payment slip file is required.");

        var savedSlip = await imageUploadService.SaveAsync(dto.SlipFile, "payment-slips", cancellationToken);
        if (savedSlip == null) return ServiceResult<Order>.Failure(ServiceStatus.Invalid, "Only JPG, PNG, or WEBP images up to 5 MB are allowed.");

        order.SlipImageUrl = savedSlip.Url;
        order.SlipUploadedAt = dto.TransferTime ?? DateTime.UtcNow;
        order.PaymentStatus = PaymentStatuses.PendingVerification;
        unit.Repository<Order>().Update(order);

        if (await unit.Complete()) return ServiceResult<Order>.Success(order);

        imageUploadService.Delete(savedSlip);
        return ServiceResult<Order>.Failure(ServiceStatus.Failed, "Unable to save the payment slip.");
    }
}
