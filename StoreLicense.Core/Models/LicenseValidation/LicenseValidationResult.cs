namespace StoreLicense.Core.Models.LicenseValidation;

public class LicenseValidationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid? SubscriberId { get; set; }

    public Guid? SubscriptionId { get; set; }

    public Guid? DeviceId { get; set; }

    public DateTime? SubscriptionStartDate { get; set; }

    public DateTime? SubscriptionEndDate { get; set; }

    public bool IsLifetime { get; set; }

    public DateTime ServerTime { get; set; }

    public DateTime? LicenseExpiresAt { get; set; }

    public DateTime? NextValidationAt { get; set; }

    public int GracePeriodDays { get; set; }

    public string LicenseToken { get; set; } = string.Empty;
}