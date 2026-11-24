
using System.ComponentModel.DataAnnotations;

namespace StoreLicense.Api.Contracts.Activation;

public class ActivateRequest
{
    [Required(ErrorMessage = "رمز التفعيل مطلوب")]
    public string ActivationCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "معرف الجهاز مطلوب")]
    [StringLength(200, MinimumLength = 5,
        ErrorMessage = "معرف الجهاز يجب أن يكون بين 5 و200 حرف")]
    public string DeviceIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم الجهاز مطلوب")]
    [StringLength(150, MinimumLength = 2,
        ErrorMessage = "اسم الجهاز يجب أن يكون بين حرفين و150 حرفاً")]
    public string DeviceName { get; set; } = string.Empty;

    [Required(ErrorMessage = "المفتاح العام للجهاز مطلوب")]
    [StringLength(256, MinimumLength = 20,
        ErrorMessage = "المفتاح العام غير صالح")]
    public string DevicePublicKey { get; set; } = string.Empty;
}
