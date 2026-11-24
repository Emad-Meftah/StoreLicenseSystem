using System.ComponentModel.DataAnnotations;

namespace StoreLicense.Api.Contracts.LicenseValidation;

public class ValidateLicenseRequest
{
    [Required(ErrorMessage = "معرف الجهاز مطلوب")]
    public Guid DeviceId { get; set; }

    [Required(ErrorMessage = "معرف الجهاز الفريد مطلوب")]
    [StringLength(200, MinimumLength = 5)]
    public string DeviceIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "وقت الطلب مطلوب")]
    public long Timestamp { get; set; }

    [Required(ErrorMessage = "رقم الطلب الفريد مطلوب")]
    public Guid Nonce { get; set; }

    [Required(ErrorMessage = "التوقيع الرقمي مطلوب")]
    public string Signature { get; set; } = string.Empty;
}