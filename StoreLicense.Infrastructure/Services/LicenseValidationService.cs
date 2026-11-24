using Microsoft.EntityFrameworkCore;
using Npgsql;
using StoreLicense.Core.Entities;
using StoreLicense.Core.Enums;
using StoreLicense.Core.Interfaces;
using StoreLicense.Core.Models.LicenseValidation;
using StoreLicense.Infrastructure.Data;
using StoreLicense.Infrastructure.Security;

namespace StoreLicense.Infrastructure.Services;

public class LicenseValidationService : ILicenseValidationService
{
    private readonly AppDbContext _context;
    private readonly ILicenseTokenService _licenseTokenService;

    private const int AllowedClockDifferenceSeconds = 300;
    private const int NonceRetentionMinutes = 10;
    private const int GracePeriodDays = 7;

    public LicenseValidationService(
        AppDbContext context,
        ILicenseTokenService licenseTokenService)
    {
        _context = context;
        _licenseTokenService = licenseTokenService;
    }

    public async Task<LicenseValidationResult> ValidateAsync(
        ValidateLicenseCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // 1. التحقق من البيانات الأساسية
        if (command.DeviceId == Guid.Empty ||
            command.Nonce == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.DeviceIdentifier) ||
            string.IsNullOrWhiteSpace(command.Signature))
        {
            return Failure("بيانات طلب التحقق غير مكتملة.", now);
        }

        // 2. التحقق من توقيت الطلب
        var currentTimestamp =
            DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (command.Timestamp <
                currentTimestamp - AllowedClockDifferenceSeconds ||
            command.Timestamp >
                currentTimestamp + AllowedClockDifferenceSeconds)
        {
            return Failure(
                "انتهت صلاحية طلب التحقق. يرجى المحاولة مجدداً.",
                now);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);


        // 3. قفل سجل الجهاز لمنع التداخل مع الإلغاء أو النقل.
        var device = await _context.SubscriberDevices
            .FromSqlInterpolated($"""
                SELECT *
                FROM subscriber_devices
                WHERE "Id" = {command.DeviceId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (device is null)
        {
            return await RollbackFailureAsync(
                transaction,
                "الجهاز غير مسجل في النظام.",
                now,
                cancellationToken);
        }

        // 4. قفل الاشتراك بعد قفل الجهاز.
        // ترتيب الأقفال متوافق مع ActivationService.
        var subscription = await _context.Subscriptions
            .FromSqlInterpolated($"""
                SELECT *
                FROM subscriptions
                WHERE "Id" = {device.SubscriptionId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return await RollbackFailureAsync(
                transaction,
                "الاشتراك المرتبط بالجهاز غير موجود.",
                now,
                cancellationToken);
        }

        // تحميل بيانات المشترك.
        // قفل سجل المشترك لمنع تغيير حالته أثناء التحقق.
        var subscriber = await _context.Subscribers
            .FromSqlInterpolated($"""
                SELECT *
                FROM subscribers
                WHERE "Id" = {device.SubscriberId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (subscriber is null)
        {
            return await RollbackFailureAsync(
                transaction,
                "المشترك المرتبط بالجهاز غير موجود.",
                now,
                cancellationToken);
        }

        // ربط الكيانات المحملة بالجهاز.
        device.Subscription = subscription;
        device.Subscriber = subscriber;


        // 4. التحقق من حالة الجهاز
        if (device.Status != DeviceStatus.Active)
        {
            return await RollbackFailureAsync(
                transaction,
                "هذا الجهاز غير نشط أو تم إلغاء تفعيله.",
                now,
                cancellationToken);
        }

        // 5. مطابقة معرف الجهاز
        if (!string.Equals(
                device.DeviceIdentifier,
                command.DeviceIdentifier,
                StringComparison.Ordinal))
        {
            return await RollbackFailureAsync(
                transaction,
                "معرف الجهاز غير مطابق.",
                now,
                cancellationToken);
        }

        // 6. التحقق من التوقيع الرقمي
        var signatureIsValid = LicenseSignatureVerifier.Verify(
            device.DevicePublicKey,
            command.Signature,
            command.DeviceId,
            command.DeviceIdentifier,
            command.Timestamp,
            command.Nonce);

        if (!signatureIsValid)
        {
            return await RollbackFailureAsync(
                transaction,
                "فشل التحقق من هوية الجهاز.",
                now,
                cancellationToken);
        }

        // 7. حذف أرقام الطلبات المنتهية لهذا الجهاز
        await _context.LicenseValidationNonces
            .Where(n =>
                n.DeviceId == device.Id &&
                n.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        // 8. تسجيل رقم الطلب لمنع إعادة استخدامه
        var nonceRecord = new LicenseValidationNonce
        {
            DeviceId = device.Id,
            Nonce = command.Nonce,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(NonceRetentionMinutes)
        };

        _context.LicenseValidationNonces.Add(nonceRecord);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException postgresException
                  && postgresException.SqlState ==
                     PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure(
                "تم رفض الطلب لأنه سبق استخدامه.",
                now);
        }

        // 9. التحقق من حالة المشترك
        if (device.Subscriber.Status != SubscriberStatus.Active)
        {
            await transaction.CommitAsync(cancellationToken);

            return Failure(
                "اشتراك المتجر موقوف أو محظور.",
                now);
        }

        // 10. التحقق من حالة الاشتراك
        if (subscription.Status == SubscriptionStatus.Expired)
        {
            await transaction.CommitAsync(cancellationToken);

            return Failure(
                "انتهت صلاحية الاشتراك.",
                now);
        }

        if (subscription.Status != SubscriptionStatus.Active)
        {
            await transaction.CommitAsync(cancellationToken);
            return Failure(
                "الاشتراك غير نشط حالياً.",
                now);
        }

        // 11. التحقق من تاريخ بداية الاشتراك
        if (subscription.StartDate is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return Failure(
                "لم يتم تحديد تاريخ بداية الاشتراك.",
                now);
        }

        // 12. التحقق من انتهاء الاشتراك
        if (!subscription.IsLifetime &&
            subscription.EndDate <= now)
        {
            subscription.Status = SubscriptionStatus.Expired;
            _context.LicenseAuditLogs.Add(new LicenseAuditLog
            {
                SubscriberId = device.SubscriberId,
                SubscriptionId = device.SubscriptionId,
                Action = "SubscriptionExpired",
                Details = "انتهت مدة الاشتراك أثناء طلب التحقق.",
                CreatedAt = now
            });
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Failure(
                "انتهت صلاحية الاشتراك.",
                now);
        }
        // 13. تحديث آخر ظهور للجهاز
        device.LastSeenAt = now;
        // 14. تسجيل عملية التحقق
        _context.LicenseAuditLogs.Add(new LicenseAuditLog
        {
            SubscriberId = device.SubscriberId,
            SubscriptionId = device.SubscriptionId,
            Action = "LicenseValidated",
            Details = $"تم التحقق من ترخيص الجهاز: {device.DeviceName}",
            CreatedAt = now
        });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        // إنشاء بيانات الترخيص
        var licenseExpiresAt = now.AddHours(24);
        var nextValidationAt = now.AddHours(12);
        var payload = new LicenseTokenPayload
        {
            LicenseVersion = 1,

            SubscriberId = device.SubscriberId,
            SubscriptionId = device.SubscriptionId,
            DeviceId = device.Id,

            IssuedAt = now,
            ExpiresAt = licenseExpiresAt,
            NextValidationAt = nextValidationAt,

            SubscriptionStartDate = subscription.StartDate,
            SubscriptionEndDate = subscription.EndDate,

            IsLifetime = subscription.IsLifetime,
            MaxDevices = subscription.MaxDevices,

            GracePeriodDays = GracePeriodDays
        };

        // توقيع الترخيص بالمفتاح الخاص للخادم
        var licenseToken =
            _licenseTokenService.GenerateToken(payload);

        return new LicenseValidationResult
        {
            Success = true,
            Message = "الترخيص صالح.",

            SubscriberId = device.SubscriberId,
            SubscriptionId = device.SubscriptionId,
            DeviceId = device.Id,

            SubscriptionStartDate = subscription.StartDate,
            SubscriptionEndDate = subscription.EndDate,

            IsLifetime = subscription.IsLifetime,

            ServerTime = now,
            LicenseExpiresAt = licenseExpiresAt,
            NextValidationAt = nextValidationAt,

            GracePeriodDays = GracePeriodDays,

            LicenseToken = licenseToken
        };
    }

    private static LicenseValidationResult Failure(
        string message,
        DateTime serverTime)
    {
        return new LicenseValidationResult
        {
            Success = false,
            Message = message,
            ServerTime = serverTime,
            GracePeriodDays = GracePeriodDays
        };
    }

    private static async Task<LicenseValidationResult>
        RollbackFailureAsync(
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction
                transaction,
            string message,
            DateTime serverTime,
            CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);

        return Failure(message, serverTime);
    }
}