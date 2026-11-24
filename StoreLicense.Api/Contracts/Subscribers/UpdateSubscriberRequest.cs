using System.ComponentModel.DataAnnotations;

namespace StoreLicense.Api.Contracts.Subscribers;

public class UpdateSubscriberRequest
{
    [Required(ErrorMessage = "اسم المتجر مطلوب")]
    [StringLength(150, MinimumLength = 2,
        ErrorMessage = "اسم المتجر يجب أن يكون بين حرفين و150 حرفاً")]
    public string StoreName { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم صاحب المتجر مطلوب")]
    [StringLength(150, MinimumLength = 2,
        ErrorMessage = "اسم صاحب المتجر يجب أن يكون بين حرفين و150 حرفاً")]
    public string OwnerName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Notes { get; set; }
}