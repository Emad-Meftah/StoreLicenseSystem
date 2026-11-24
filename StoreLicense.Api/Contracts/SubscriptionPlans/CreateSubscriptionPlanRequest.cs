using System.ComponentModel.DataAnnotations;

namespace StoreLicense.Api.Contracts.SubscriptionPlans;

public class CreateSubscriptionPlanRequest
{
    [Required(ErrorMessage = "اسم الخطة مطلوب")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "اسم الخطة يجب أن يكون بين حرفين و100 حرف")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "يجب تحديد نوع الخطة")]
    public bool IsLifetime { get; set; }

    [Range(1, 36500, ErrorMessage = "مدة الاشتراك غير صحيحة")]
    public int? DurationDays { get; set; }

    [Range(1, 1000, ErrorMessage = "عدد الأجهزة يجب أن يكون أكبر من صفر")]
    public int DefaultMaxDevices { get; set; } = 1;
}