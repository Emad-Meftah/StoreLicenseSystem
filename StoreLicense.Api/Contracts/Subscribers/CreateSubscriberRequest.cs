using System.ComponentModel.DataAnnotations;

namespace StoreLicense.Api.Contracts.Subscribers;

public class CreateSubscriberRequest
{
    [Required(ErrorMessage = "اسم المتجر مطلوب")]
    public string StoreName { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم صاحب المتجر مطلوب")]
    public string OwnerName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Notes { get; set; }
}