using Microsoft.EntityFrameworkCore;
using StoreLicense.Core.Entities;
using StoreLicense.Core.Enums;
using StoreLicense.Core.Interfaces;
using StoreLicense.Core.Models.Activation;
using StoreLicense.Infrastructure.Data;
using StoreLicense.Infrastructure.Security;
using Npgsql;

namespace StoreLicense.Infrastructure.Services;

public class ActivationService : IActivationService
{
    private readonly AppDbContext _context;

    public ActivationService(AppDbContext context)
    {
        _context = context;
    }


    public async Task<ActivationResult> ActivateAsync(
        ActivateCommand command,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ActivationCode) ||
            string.IsNullOrWhiteSpace(command.DeviceIdentifier) ||
            string.IsNullOrWhiteSpace(command.DeviceName) ||
            string.IsNullOrWhiteSpace(command.DevicePublicKey))
        {
            return Failure("بيانات التفعيل غير مكتملة.");
        }

        var now = DateTime.UtcNow;

        var codeHash = ActivationCodeGenerator.ComputeHash(
            command.ActivationCode.Trim());

        var deviceIdentifier = command.DeviceIdentifier.Trim();
        var devicePublicKey = command.DevicePublicKey.Trim();
        var deviceName = command.DeviceName.Trim();
        // التحقق من أطوال بيانات الجهاز.
        if (deviceName.Length > 150)
        {
            return Failure("اسم الجهاز يتجاوز الحد المسموح (150 حرفاً).");
        }
        if (devicePublicKey.Length > 256)
        {
            return Failure("المفتاح العام يتجاوز الحد المسموح (256 حرفاً).");
        }
        if (deviceIdentifier.Length > 200)
        {
            return Failure("معرف الجهاز يتجاوز الحد المسموح (200 حرف).");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            // 1. قفل رمز التفعيل.
            var activationCode = await _context.ActivationCodes
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM activation_codes
                    WHERE "CodeHash" = {codeHash}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (activationCode is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure("رمز التفعيل غير صحيح.");
            }

            // 2. قراءة الاشتراك المستهدف دون قفله.
            var targetSubscriptionInfo = await _context.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == activationCode.SubscriptionId,
                    cancellationToken);

            if (targetSubscriptionInfo is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure("الاشتراك غير موجود.");
            }

            // 3. قفل خاص بالمفتاح العام لمنع التفعيل المتزامن للجهاز نفسه.
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                SELECT pg_advisory_xact_lock(
                    hashtextextended({devicePublicKey}, 0)
                )
                """,
                cancellationToken);

            // 4. البحث عن الجهاز السابق وقفل سجله.
            var previousDevice = await _context.SubscriberDevices
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM subscriber_devices
                    WHERE "DevicePublicKey" = {devicePublicKey}
                    AND "Status" = {(int)DeviceStatus.Active}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (previousDevice is not null &&
                previousDevice.DeviceIdentifier != deviceIdentifier)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    "المفتاح العام مرتبط بمعرف جهاز مختلف.");
            }

            // 5. تحديد الاشتراكات المطلوب قفلها.
            var subscriptionIds = new List<Guid>
            {
                targetSubscriptionInfo.Id
            };
            if (previousDevice is not null &&
                previousDevice.SubscriptionId != targetSubscriptionInfo.Id)
            {
                subscriptionIds.Add(previousDevice.SubscriptionId);
            }

            // قفل الاشتراكات بترتيب ثابت حسب Guid لتجنب Deadlock.
            var lockedSubscriptions = new Dictionary<Guid, Subscription>();
            foreach (var subscriptionId in subscriptionIds
                         .Distinct()
                         .OrderBy(x => x))
            {
                var lockedSubscription = await _context.Subscriptions
                    .FromSqlInterpolated($"""
                        SELECT *
                        FROM subscriptions
                        WHERE "Id" = {subscriptionId}
                        FOR UPDATE
                        """)
                    .SingleOrDefaultAsync(cancellationToken);
                if (lockedSubscription is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure("تعذر العثور على أحد الاشتراكات.");
                }
                lockedSubscriptions.Add(
                    subscriptionId,
                    lockedSubscription);
            }
            var subscription =
                lockedSubscriptions[targetSubscriptionInfo.Id];
            Subscription? previousSubscription = null;
            if (previousDevice is not null)
            {
                previousSubscription =
                    lockedSubscriptions[previousDevice.SubscriptionId];
            }
            // التحقق من المشترك بعد قفل الاشتراك.
            var subscriber = await _context.Subscribers
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM subscribers
                    WHERE "Id" = {subscription.SubscriberId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (subscriber is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure("بيانات المشترك غير موجودة.");
            }
            // 6. التحقق من حالة رمز التفعيل.
            if (activationCode.Status != ActivationCodeStatus.Generated)
            {
                AddAuditLog(
                    subscription,
                    "ActivationRejected",
                    "تمت محاولة استخدام رمز تفعيل غير متاح.",
                    ipAddress,
                    now);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Failure("رمز التفعيل مستخدم أو ملغى أو منتهي.");
            }

            // التحقق من صلاحية الرمز.
            if (activationCode.ExpiresAt.HasValue &&
                activationCode.ExpiresAt.Value <= now)
            {
                activationCode.Status = ActivationCodeStatus.Expired;

                AddAuditLog(
                    subscription,
                    "ActivationExpired",
                    "انتهت صلاحية رمز التفعيل.",
                    ipAddress,
                    now);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Failure("انتهت صلاحية رمز التفعيل.");
            }

            // التحقق من حالة المشترك.
            if (subscriber.Status != SubscriberStatus.Active)
            {
                AddAuditLog(
                    subscription,
                    "ActivationRejected",
                    "المشترك غير نشط.",
                    ipAddress,
                    now);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Failure("حساب المشترك غير نشط.");
            }

            // التحقق من حالة الاشتراك.
            if (subscription.Status != SubscriptionStatus.Pending &&
                subscription.Status != SubscriptionStatus.Active)
            {
                AddAuditLog(
                    subscription,
                    "ActivationRejected",
                    "الاشتراك غير متاح للتفعيل.",
                    ipAddress,
                    now);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Failure("الاشتراك غير متاح للتفعيل.");
            }

            // التحقق من انتهاء الاشتراك المستهدف.
            if (!subscription.IsLifetime &&
                subscription.EndDate.HasValue &&
                subscription.EndDate.Value <= now)
            {
                subscription.Status = SubscriptionStatus.Expired;

                AddAuditLog(
                    subscription,
                    "SubscriptionExpired",
                    "انتهت مدة الاشتراك.",
                    ipAddress,
                    now);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Failure("انتهت مدة الاشتراك.");
            }

            // البحث عن الجهاز نفسه داخل الاشتراك المستهدف.
            var existingDevice = await _context.SubscriberDevices
                .FirstOrDefaultAsync(
                    x => x.SubscriptionId == subscription.Id &&
                         x.DeviceIdentifier == deviceIdentifier,
                    cancellationToken);
            if (existingDevice is not null)
            {
                if (existingDevice.Status != DeviceStatus.Active)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure("هذا الجهاز تم إلغاؤه سابقاً.");
                }

                if (existingDevice.DevicePublicKey != devicePublicKey)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure("مفتاح الجهاز لا يطابق الجهاز المسجل.");
                }

                activationCode.Status = ActivationCodeStatus.Redeemed;
                activationCode.RedeemedAt = now;
                existingDevice.LastSeenAt = now;

                var existingCount = await _context.SubscriberDevices
                    .CountAsync(
                        x => x.SubscriptionId == subscription.Id &&
                             x.Status == DeviceStatus.Active,
                        cancellationToken);

                AddAuditLog(
                    subscription,
                    "DeviceActivationConfirmed",
                    "تم تأكيد تفعيل جهاز مسجل مسبقاً.",
                    ipAddress,
                    now);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Success(
                    subscription,
                    existingDevice,
                    existingCount,
                    now);
            }


            // التحقق من مدة الاشتراك قبل بدء التفعيل أو نقل الجهاز.
            if (subscription.Status == SubscriptionStatus.Pending &&
                !subscription.IsLifetime &&
                (!subscription.DurationDays.HasValue ||
                subscription.DurationDays.Value <= 0))
            {
                AddAuditLog(
                    subscription,
                    "ActivationRejected",
                    "تعذر التفعيل: مدة الاشتراك غير محددة أو غير صالحة.",
                    ipAddress,
                    now);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Failure("مدة الاشتراك غير صالحة. يرجى مراجعة إدارة الاشتراكات.");
            }

            // حساب الأجهزة النشطة في الاشتراك الجديد.
            var activeDevicesCount = await _context.SubscriberDevices
                .CountAsync(
                    x => x.SubscriptionId == subscription.Id &&
                         x.Status == DeviceStatus.Active,
                    cancellationToken);

            // فحص السعة قبل تغيير حالة الجهاز السابق.
            if (activeDevicesCount >= subscription.MaxDevices)
            {
                AddAuditLog(
                    subscription,
                    "ActivationRejected",
                    "تم تجاوز الحد الأقصى للأجهزة المسموح بها.",
                    ipAddress,
                    now);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Failure("تم الوصول إلى الحد الأقصى للأجهزة.");
            }

            var wasTransferred = false;

            if (previousDevice is not null)
            {
                if (previousDevice.SubscriptionId == subscription.Id)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return Failure(
                        "الجهاز مسجل مسبقاً في هذا الاشتراك.");
                }

                // تحديث الاشتراك السابق إذا انتهت مدته فعلياً.
                if (previousSubscription!.Status == SubscriptionStatus.Active &&
                    !previousSubscription.IsLifetime &&
                    previousSubscription.EndDate.HasValue &&
                    previousSubscription.EndDate.Value <= now)
                {
                    previousSubscription.Status =
                        SubscriptionStatus.Expired;

                    AddAuditLog(
                        previousSubscription,
                        "SubscriptionExpired",
                        "تم اعتبار الاشتراك منتهياً أثناء محاولة نقل الجهاز.",
                        ipAddress,
                        now);
                }

                // السماح بالنقل فقط من اشتراك منتهي أو ملغى.
                if (previousSubscription.Status != SubscriptionStatus.Expired &&
                    previousSubscription.Status != SubscriptionStatus.Cancelled)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return Failure(
                        "الجهاز مرتبط باشتراك آخر لا يزال صالحاً أو غير مؤهل للنقل.");
                }

                // الاحتفاظ بالسجل القديم وإلغاء تفعيل الجهاز فيه.
                previousDevice.Status = DeviceStatus.Revoked;

                AddAuditLog(
                    previousSubscription,
                    "DeviceTransferredOut",
                    $"تم نقل الجهاز {previousDevice.DeviceName} إلى اشتراك آخر.",
                    ipAddress,
                    now);

                wasTransferred = true;
            }

            // إنشاء سجل الجهاز في الاشتراك الجديد.
            var device = new SubscriberDevice
            {
                SubscriberId = subscription.SubscriberId,
                SubscriptionId = subscription.Id,
                DeviceIdentifier = deviceIdentifier,
                DeviceName = deviceName,
                DevicePublicKey = devicePublicKey,
                ActivatedAt = now,
                LastSeenAt = now,
                Status = DeviceStatus.Active
            };

            _context.SubscriberDevices.Add(device);

            // بدء مدة الاشتراك عند أول تفعيل فقط.
            if (subscription.Status == SubscriptionStatus.Pending)
            {
                subscription.StartDate = now;

                subscription.EndDate = subscription.IsLifetime
                    ? null
                    : now.AddDays(subscription.DurationDays!.Value);

                subscription.Status = SubscriptionStatus.Active;
            }

            // استهلاك رمز التفعيل.
            activationCode.Status = ActivationCodeStatus.Redeemed;
            activationCode.RedeemedAt = now;

            AddAuditLog(
                subscription,
                "DeviceActivated",
                $"تم تفعيل الجهاز: {device.DeviceName}",
                ipAddress,
                now);

            if (wasTransferred)
            {
                AddAuditLog(
                    subscription,
                    "DeviceTransferredIn",
                    $"تم نقل الجهاز {device.DeviceName} من اشتراك سابق إلى هذا الاشتراك.",
                    ipAddress,
                    now);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Success(
                subscription,
                device,
                activeDevicesCount + 1,
                now);
        }
                catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            if (ex.GetBaseException() is PostgresException pgEx)
            {
                if (pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    return Failure(
                        "الجهاز مسجل مسبقاً أو توجد عملية تفعيل متزامنة. حاول مجدداً.");
                }

                if (pgEx.SqlState == PostgresErrorCodes.DeadlockDetected)
                {
                    return Failure(
                        "حدث تعارض مؤقت أثناء التفعيل. يرجى المحاولة مجدداً.");
                }
            }

            return Failure(
                "تعذر حفظ بيانات التفعيل. تحقق من البيانات وحاول مجدداً.");
        }
        catch (PostgresException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (ex.SqlState == PostgresErrorCodes.DeadlockDetected)
            {
                return Failure(
                    "حدث تعارض مؤقت أثناء التفعيل. يرجى المحاولة مجدداً.");
            }
            return Failure(
                "حدث خطأ في قاعدة البيانات أثناء التفعيل.");
        }
    }


    private void AddAuditLog(
        Subscription subscription,
        string action,
        string details,
        string? ipAddress,
        DateTime now)
    {
        _context.LicenseAuditLogs.Add(new LicenseAuditLog
        {
            SubscriberId = subscription.SubscriberId,
            SubscriptionId = subscription.Id,
            Action = action,
            Details = details,
            IpAddress = ipAddress,
            CreatedAt = now
        });
    }

    private static ActivationResult Success(
        Subscription subscription,
        SubscriberDevice device,
        int activeDevicesCount,
        DateTime now)
    {
        return new ActivationResult
        {
            Success = true,
            Message = "تم تفعيل الاشتراك بنجاح.",

            SubscriberId = subscription.SubscriberId,
            SubscriptionId = subscription.Id,
            DeviceId = device.Id,

            SubscriptionStatus = subscription.Status,

            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,

            IsLifetime = subscription.IsLifetime,
            MaxDevices = subscription.MaxDevices,
            ActiveDevicesCount = activeDevicesCount,

            ActivatedAt = now
        };
    }

    private static ActivationResult Failure(string message)
    {
        return new ActivationResult
        {
            Success = false,
            Message = message
        };
    }
}
