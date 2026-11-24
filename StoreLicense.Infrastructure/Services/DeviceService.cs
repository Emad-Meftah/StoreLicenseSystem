
using Microsoft.EntityFrameworkCore;
using StoreLicense.Core.Entities;
using StoreLicense.Core.Enums;
using StoreLicense.Core.Interfaces;
using StoreLicense.Infrastructure.Data;

namespace StoreLicense.Infrastructure.Services;

public class DeviceService : IDeviceService
{
    private readonly AppDbContext _context;

    public DeviceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SubscriberDevice>>
        GetBySubscriptionAsync(
            Guid subscriptionId,
            CancellationToken cancellationToken = default)
    {
        return await _context.SubscriberDevices
            .AsNoTracking()
            .Where(d => d.SubscriptionId == subscriptionId)
            .OrderByDescending(d => d.ActivatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevokeAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var device = await _context.SubscriberDevices
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM subscriber_devices
                    WHERE "Id" = {deviceId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (device is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (device.Status == DeviceStatus.Revoked)
            {
                await transaction.CommitAsync(cancellationToken);
                return true;
            }

            device.Status = DeviceStatus.Revoked;

            var now = DateTime.UtcNow;

            var auditLog = new LicenseAuditLog
            {
                SubscriberId = device.SubscriberId,
                SubscriptionId = device.SubscriptionId,
                Action = "DeviceRevoked",
                Details = $"تم إلغاء تفعيل الجهاز: {device.DeviceName}",
                CreatedAt = now
            };

            _context.LicenseAuditLogs.Add(auditLog);

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

    public async Task<bool> DeleteAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var device = await _context.SubscriberDevices
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM subscriber_devices
                    WHERE "Id" = {deviceId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (device is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var auditLog = new LicenseAuditLog
            {
                SubscriberId = device.SubscriberId,
                SubscriptionId = device.SubscriptionId,
                Action = "DeviceDeletedForTesting",
                Details =
                    $"حذف سجل جهاز للاختبار: {device.DeviceName} " +
                    $"(DeviceId: {device.Id})",
                CreatedAt = DateTime.UtcNow
            };

            _context.LicenseAuditLogs.Add(auditLog);
            _context.SubscriberDevices.Remove(device);

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
}
