namespace StoreLicense.Core.Models.LicenseValidation;

public class LicenseTokenPayload
{
    public int LicenseVersion { get; set; } = 1;

    public Guid SubscriberId { get; set; }

    public Guid SubscriptionId { get; set; }

    public Guid DeviceId { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime NextValidationAt { get; set; }

    public DateTime? SubscriptionStartDate { get; set; }

    public DateTime? SubscriptionEndDate { get; set; }

    public bool IsLifetime { get; set; }

    public int MaxDevices { get; set; }

    public int GracePeriodDays { get; set; } = 7;
}