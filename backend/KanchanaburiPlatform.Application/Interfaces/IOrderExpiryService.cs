namespace Core.Interfaces;

public interface IOrderExpiryService
{
    Task<int> ExpirePendingPaymentsAsync(CancellationToken cancellationToken = default);
    Task<bool> ExpireIfNeededAsync(Guid orderId, CancellationToken cancellationToken = default);
}
