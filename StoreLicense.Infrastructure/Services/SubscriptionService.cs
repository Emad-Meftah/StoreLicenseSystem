
using Microsoft.EntityFrameworkCore;
using StoreLicense.Core.Entities;
using StoreLicense.Core.Enums;
using StoreLicense.Core.Interfaces;
using StoreLicense.Infrastructure.Data;
using StoreLicense.Infrastructure.Security;
using StoreLicense.Core.Models.Activation;

namespace StoreLicense.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _context;
    private readonly ActivationCodeProtectionService _activationCodeProtection;

    public SubscriptionService(
        AppDbContext context,
        ActivationCodeProtectionService activationCodeProtection)
    {
        _context = context;
        _activationCodeProtection = activationCodeProtection;
    }

    public async Task<(Subscription Subscription, string ActivationCode)?> CreateAsync(
        Guid subscriberId,
        Guid planId,
        int? maxDevices,
        DateTime? activationCodeExpiresAt,
        CancellationToken cancellationToken = default)
    {
        // التحقق من المشترك
        var subscriber = await _context.Subscribers
            .FirstOrDefaultAsync(
                x => x.Id == subscriberId,
                cancellationToken);

        if (subscriber is null ||
            subscriber.Status != SubscriberStatus.Active)
        {
            throw new InvalidOperationException(
                "المشترك غير موجود أو غير نشط.");
        }

        // التحقق من الخطة
        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(
                x => x.Id == planId,
                cancellationToken);

        if (plan is null || !plan.IsActive)
        {
            throw new InvalidOperationException(
                "خطة الاشتراك غير موجودة أو غير نشطة.");
        }

        // تحديد عدد الأجهزة
        var deviceLimit = maxDevices ?? plan.DefaultMaxDevices;

        if (deviceLimit < 1 || deviceLimit > 1000)
        {
            throw new InvalidOperationException(
                "عدد الأجهزة يجب أن يكون بين 1 و1000.");
        }

        // التحقق من تاريخ انتهاء رمز التفعيل
        var now = DateTime.UtcNow;

        if (activationCodeExpiresAt.HasValue)
        {
            if (activationCodeExpiresAt.Value.Kind != DateTimeKind.Utc ||
                activationCodeExpiresAt.Value <= now)
            {
                throw new InvalidOperationException(
                    "تاريخ انتهاء رمز التفعيل يجب أن يكون مستقبلياً وبصيغة UTC.");
            }
        }

        // إنشاء الاشتراك دون بدء مدة الاشتراك
        var subscription = new Subscription
        {
            SubscriberId = subscriberId,
            Subscriber = subscriber,

            PlanId = plan.Id,
            Plan = plan,

            IsLifetime = plan.IsLifetime,
            DurationDays = plan.DurationDays,

            MaxDevices = deviceLimit,

            Status = SubscriptionStatus.Pending,

            StartDate = null,
            EndDate = null,

            CreatedAt = now
        };

        // توليد رمز التفعيل
        var activationCode = ActivationCodeGenerator.Generate();

        var activationCodeEntity = new ActivationCode
        {
            SubscriptionId = subscription.Id,

            CodeHash = ActivationCodeGenerator.ComputeHash(
                activationCode),

            Status = ActivationCodeStatus.Generated,

            CreatedAt = now,

            ExpiresAt = activationCodeExpiresAt
        };

        subscription.ActivationCodes.Add(activationCodeEntity);

        // حفظ الاشتراك ورمز التفعيل معاً
        _context.Subscriptions.Add(subscription);

        await _context.SaveChangesAsync(cancellationToken);

        // إعادة الرمز الأصلي مرة واحدة فقط
        return (subscription, activationCode);
    }



    public async Task<GeneratedActivationCode?> GenerateActivationCodeAsync(
    Guid subscriptionId,
    DateTime? expiresAt,
    CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // التحقق من تاريخ انتهاء الرمز
        if (expiresAt.HasValue)
        {
            if (expiresAt.Value.Kind != DateTimeKind.Utc ||
                expiresAt.Value <= now)
            {
                throw new InvalidOperationException(
                    "تاريخ انتهاء رمز التفعيل يجب أن يكون مستقبلياً وبصيغة UTC.");
            }
        }

        // جلب الاشتراك مع المشترك
        var subscription = await _context.Subscriptions
            .Include(x => x.Subscriber)
            .FirstOrDefaultAsync(
                x => x.Id == subscriptionId,
                cancellationToken);

        if (subscription is null)
        {
            return null;
        }

        // التحقق من حالة المشترك
        if (subscription.Subscriber.Status != SubscriberStatus.Active)
        {
            throw new InvalidOperationException(
                "لا يمكن إصدار رمز تفعيل لمشترك غير نشط.");
        }

        // التحقق من حالة الاشتراك
        if (subscription.Status != SubscriptionStatus.Pending &&
            subscription.Status != SubscriptionStatus.Active)
        {
            throw new InvalidOperationException(
                "لا يمكن إصدار رمز تفعيل لهذا الاشتراك.");
        }

        // التحقق من انتهاء مدة الاشتراك
        if (!subscription.IsLifetime &&
            subscription.EndDate.HasValue &&
            subscription.EndDate.Value <= now)
        {
            throw new InvalidOperationException(
                "انتهت مدة الاشتراك، لا يمكن إصدار رمز تفعيل جديد.");
        }

        // توليد رمز جديد
        var activationCode = ActivationCodeGenerator.Generate();

        var activationCodeEntity = new ActivationCode
        {
            SubscriptionId = subscription.Id,

            CodeHash = ActivationCodeGenerator.ComputeHash(
                activationCode),
            EncryptedCode = _activationCodeProtection.Protect(
                activationCode),
            Status = ActivationCodeStatus.Generated,

            CreatedAt = now,

            ExpiresAt = expiresAt
        };

        _context.ActivationCodes.Add(activationCodeEntity);

        // تسجيل العملية في سجل التدقيق
        _context.LicenseAuditLogs.Add(new LicenseAuditLog
        {
            SubscriberId = subscription.SubscriberId,
            SubscriptionId = subscription.Id,
            Action = "ActivationCodeGenerated",
            Details = "تم إصدار رمز تفعيل إضافي للاشتراك.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        // إعادة الرمز الأصلي مرة واحدة فقط
        return new GeneratedActivationCode
        {
            SubscriptionId = subscription.Id,
            ActivationCode = activationCode,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
    }



    public async Task<IReadOnlyList<Subscription>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .Include(x => x.Subscriber)
            .Include(x => x.Plan)
            .Include(x => x.Devices)
            .Include(x => x.ActivationCodes)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Subscription?> GetByIdAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .Include(x => x.Subscriber)
            .Include(x => x.Plan)
            .Include(x => x.Devices)
            .Include(x => x.ActivationCodes)
            .FirstOrDefaultAsync(
                x => x.Id == subscriptionId,
                cancellationToken);
    }




    public async Task<bool> CancelAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var subscription = await _context.Subscriptions
                .Include(x => x.Devices)
                .Include(x => x.ActivationCodes)
                .FirstOrDefaultAsync(
                    x => x.Id == subscriptionId,
                    cancellationToken);

            if (subscription is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (subscription.Status != SubscriptionStatus.Pending &&
                subscription.Status != SubscriptionStatus.Active)
            {
                throw new InvalidOperationException(
                    "لا يمكن إلغاء الاشتراك بسبب حالته الحالية.");
            }

            var now = DateTime.UtcNow;

            // إلغاء الاشتراك.
            subscription.Status = SubscriptionStatus.Cancelled;

            _context.LicenseAuditLogs.Add(new LicenseAuditLog
            {
                SubscriberId = subscription.SubscriberId,
                SubscriptionId = subscription.Id,
                Action = "SubscriptionCancelled",
                Details = "تم إلغاء الاشتراك بواسطة الإدارة.",
                CreatedAt = now
            });

            // إلغاء الأجهزة النشطة مع الاحتفاظ بسجلاتها.
            foreach (var device in subscription.Devices)
            {
                if (device.Status != DeviceStatus.Active)
                    continue;
                device.Status = DeviceStatus.Revoked;
                _context.LicenseAuditLogs.Add(new LicenseAuditLog
                {
                    SubscriberId = subscription.SubscriberId,
                    SubscriptionId = subscription.Id,
                    Action = "DeviceRevoked",
                    Details = $"تم إلغاء تفعيل الجهاز: {device.DeviceName} بسبب إلغاء الاشتراك.",
                    CreatedAt = now
                });
            }

            // إبطال رموز التفعيل غير المستخدمة فقط.
            foreach (var activationCode in subscription.ActivationCodes)
            {
                if (activationCode.Status != ActivationCodeStatus.Generated)
                    continue;
                activationCode.Status = ActivationCodeStatus.Revoked;
                _context.LicenseAuditLogs.Add(new LicenseAuditLog
                {
                    SubscriberId = subscription.SubscriberId,
                    SubscriptionId = subscription.Id,
                    Action = "ActivationCodeRevoked",
                    Details = "تم إبطال رمز تفعيل غير مستخدم بسبب إلغاء الاشتراك.",
                    CreatedAt = now
                });
            }

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
