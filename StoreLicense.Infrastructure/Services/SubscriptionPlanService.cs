using Microsoft.EntityFrameworkCore;
using StoreLicense.Core.Entities;
using StoreLicense.Core.Interfaces;
using StoreLicense.Infrastructure.Data;

namespace StoreLicense.Infrastructure.Services;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly AppDbContext _context;

    public SubscriptionPlanService(AppDbContext context)
    {
        _context = context;
    }

    // عرض جميع الخطط
    public async Task<IReadOnlyList<SubscriptionPlan>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SubscriptionPlans
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    // عرض خطة محددة
    public async Task<SubscriptionPlan?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );
    }

    // إضافة خطة جديدة
    public async Task<SubscriptionPlan> CreateAsync(
        string name,
        int? durationDays,
        bool isLifetime,
        int defaultMaxDevices,
        CancellationToken cancellationToken = default)
    {
        ValidatePlanData(
            name,
            durationDays,
            isLifetime,
            defaultMaxDevices
        );

        name = name.Trim();

        bool nameExists = await _context.SubscriptionPlans
            .AnyAsync(
                x => x.Name == name,
                cancellationToken
            );

        if (nameExists)
        {
            throw new InvalidOperationException(
                "توجد خطة اشتراك بهذا الاسم بالفعل."
            );
        }

        var plan = new SubscriptionPlan
        {
            Name = name,
            DurationDays = durationDays,
            IsLifetime = isLifetime,
            DefaultMaxDevices = defaultMaxDevices,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.SubscriptionPlans.Add(plan);

        await _context.SaveChangesAsync(cancellationToken);

        return plan;
    }

    // تعديل خطة موجودة
    public async Task<SubscriptionPlan?> UpdateAsync(
        Guid id,
        string name,
        int? durationDays,
        bool isLifetime,
        int defaultMaxDevices,
        CancellationToken cancellationToken = default)
    {
        ValidatePlanData(
            name,
            durationDays,
            isLifetime,
            defaultMaxDevices
        );

        name = name.Trim();

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );

        if (plan is null)
        {
            return null;
        }

        bool nameExists = await _context.SubscriptionPlans
            .AnyAsync(
                x => x.Id != id && x.Name == name,
                cancellationToken
            );

        if (nameExists)
        {
            throw new InvalidOperationException(
                "يوجد اشتراك آخر يحمل هذا الاسم."
            );
        }

        plan.Name = name;
        plan.DurationDays = durationDays;
        plan.IsLifetime = isLifetime;
        plan.DefaultMaxDevices = defaultMaxDevices;

        await _context.SaveChangesAsync(cancellationToken);

        return plan;
    }

    // تفعيل أو تعطيل خطة
    public async Task<bool> SetActiveStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken
            );

        if (plan is null)
        {
            return false;
        }

        plan.IsActive = isActive;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // التحقق من صحة بيانات الخطة
    private static void ValidatePlanData(
        string name,
        int? durationDays,
        bool isLifetime,
        int defaultMaxDevices)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "اسم الخطة مطلوب."
            );
        }

        if (name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            throw new ArgumentException(
                "اسم الخطة يجب أن يكون بين حرفين و100 حرف."
            );
        }

        if (defaultMaxDevices < 1 || defaultMaxDevices > 1000)
        {
            throw new ArgumentException(
                "عدد الأجهزة يجب أن يكون بين 1 و1000."
            );
        }

        if (isLifetime && durationDays.HasValue)
        {
            throw new ArgumentException(
                "الاشتراك الدائم لا يحتاج إلى تحديد مدة."
            );
        }

        if (!isLifetime &&
            (!durationDays.HasValue ||
             durationDays.Value < 1 ||
             durationDays.Value > 36500))
        {
            throw new ArgumentException(
                "يجب تحديد مدة صحيحة للاشتراك بين يوم واحد و36500 يوم."
            );
        }
    }
}