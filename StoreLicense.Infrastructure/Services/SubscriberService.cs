using Microsoft.EntityFrameworkCore;
using StoreLicense.Core.Entities;
using StoreLicense.Core.Enums;
using StoreLicense.Core.Interfaces;
using StoreLicense.Infrastructure.Data;

namespace StoreLicense.Infrastructure.Services;

public class SubscriberService : ISubscriberService
{
    private readonly AppDbContext _context;

    public SubscriberService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Subscriber>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Subscribers
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Subscriber?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Subscribers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == id,
                cancellationToken);
    }

    public async Task<Subscriber> CreateAsync(
        string storeName,
        string ownerName,
        string? phone,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        ValidateSubscriberData(storeName, ownerName);

        var subscriber = new Subscriber
        {
            StoreName = storeName.Trim(),
            OwnerName = ownerName.Trim(),
            Phone = NormalizeOptionalValue(phone),
            Notes = NormalizeOptionalValue(notes),
            Status = SubscriberStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Subscribers.Add(subscriber);

        await _context.SaveChangesAsync(cancellationToken);

        return subscriber;
    }

    public async Task<Subscriber?> UpdateAsync(
        Guid id,
        string storeName,
        string ownerName,
        string? phone,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        ValidateSubscriberData(storeName, ownerName);

        var subscriber = await _context.Subscribers
            .FirstOrDefaultAsync(
                s => s.Id == id,
                cancellationToken);

        if (subscriber is null)
            return null;

        subscriber.StoreName = storeName.Trim();
        subscriber.OwnerName = ownerName.Trim();
        subscriber.Phone = NormalizeOptionalValue(phone);
        subscriber.Notes = NormalizeOptionalValue(notes);
        subscriber.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return subscriber;
    }


    public async Task<bool> SetStatusAsync(
        Guid id,
        SubscriberStatus status,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);
        try
        {
            var subscriber = await _context.Subscribers
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM subscribers
                    WHERE "Id" = {id}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (subscriber is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            subscriber.Status = status;
            subscriber.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }


    private static void ValidateSubscriberData(
        string storeName,
        string ownerName)
    {
        if (string.IsNullOrWhiteSpace(storeName))
            throw new ArgumentException("اسم المتجر مطلوب.");

        if (storeName.Trim().Length > 150)
            throw new ArgumentException(
                "اسم المتجر يجب ألا يتجاوز 150 حرفاً.");

        if (string.IsNullOrWhiteSpace(ownerName))
            throw new ArgumentException("اسم صاحب المتجر مطلوب.");

        if (ownerName.Trim().Length > 150)
            throw new ArgumentException(
                "اسم صاحب المتجر يجب ألا يتجاوز 150 حرفاً.");
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}