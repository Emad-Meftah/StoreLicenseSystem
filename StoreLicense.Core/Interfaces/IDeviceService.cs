
using StoreLicense.Core.Entities;

namespace StoreLicense.Core.Interfaces;

public interface IDeviceService
{
    Task<IReadOnlyList<SubscriberDevice>> GetBySubscriptionAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);
}
