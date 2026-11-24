namespace StoreLicense.Core.Entities;

public class LicenseValidationNonce
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DeviceId { get; set; }

    public Guid Nonce { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public SubscriberDevice Device { get; set; } = null!;
}