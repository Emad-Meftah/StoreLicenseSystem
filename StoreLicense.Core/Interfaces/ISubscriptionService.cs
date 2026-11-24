
using StoreLicense.Core.Entities;
using StoreLicense.Core.Models.Activation;

namespace StoreLicense.Core.Interfaces;

public interface ISubscriptionService
{
    Task<(Subscription Subscription, string ActivationCode)?> CreateAsync(
        Guid subscriberId,
        Guid planId,
        int? maxDevices,
        DateTime? activationCodeExpiresAt,
        CancellationToken cancellationToken = default);



    Task<GeneratedActivationCode?> GenerateActivationCodeAsync(
        Guid subscriptionId,
        DateTime? expiresAt,
        CancellationToken cancellationToken = default);


    Task<IReadOnlyList<Subscription>> GetAllAsync(
        CancellationToken cancellationToken = default);



    Task<Subscription?> GetByIdAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);


    Task<bool> CancelAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

}
