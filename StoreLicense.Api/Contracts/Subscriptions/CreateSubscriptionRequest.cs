
using System.ComponentModel.DataAnnotations;

namespace StoreLicense.Api.Contracts.Subscriptions;

public class CreateSubscriptionRequest
{
    [Required(ErrorMessage = "معرف المشترك مطلوب")]
    public Guid SubscriberId { get; set; }

    [Required(ErrorMessage = "معرف خطة الاشتراك مطلوب")]
    public Guid PlanId { get; set; }

    [Range(1, 1000, ErrorMessage = "عدد الأجهزة يجب أن يكون بين 1 و1000")]
    public int? MaxDevices { get; set; }

    public DateTime? ActivationCodeExpiresAt { get; set; }
}
