using StoreLicense.Core.Entities;
using StoreLicense.Core.Enums;

namespace StoreLicense.Core.Interfaces;

public interface ISubscriberService
{
    Task<IReadOnlyList<Subscriber>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Subscriber?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Subscriber> CreateAsync(
        string storeName,
        string ownerName,
        string? phone,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<Subscriber?> UpdateAsync(
        Guid id,
        string storeName,
        string ownerName,
        string? phone,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<bool> SetStatusAsync(
        Guid id,
        SubscriberStatus status,
        CancellationToken cancellationToken = default);
}