using StoreLicense.Core.Entities;

namespace StoreLicense.Core.Interfaces;

public interface ISubscriptionPlanService
{
    // عرض جميع خطط الاشتراك
    Task<IReadOnlyList<SubscriptionPlan>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    // عرض خطة محددة
    Task<SubscriptionPlan?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    // إضافة خطة جديدة
    Task<SubscriptionPlan> CreateAsync(
        string name,
        int? durationDays,
        bool isLifetime,
        int defaultMaxDevices,
        CancellationToken cancellationToken = default
    );

    // تعديل بيانات خطة
    Task<SubscriptionPlan?> UpdateAsync(
        Guid id,
        string name,
        int? durationDays,
        bool isLifetime,
        int defaultMaxDevices,
        CancellationToken cancellationToken = default
    );

    // تفعيل أو تعطيل خطة
    Task<bool> SetActiveStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default
    );
}