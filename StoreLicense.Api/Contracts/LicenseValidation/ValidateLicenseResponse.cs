namespace StoreLicense.Api.Contracts.LicenseValidation;

public class ValidateLicenseResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string LicenseToken { get; set; } = string.Empty;

    public DateTime ServerTime { get; set; }

    public DateTime? LicenseExpiresAt { get; set; }

    public DateTime? NextValidationAt { get; set; }

    public int GracePeriodDays { get; set; }
}