namespace StoreLicense.Core.Entities;

public class LicenseAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SubscriberId { get; set; }

    public Guid? SubscriptionId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Subscriber Subscriber { get; set; } = null!;

    public Subscription? Subscription { get; set; }
}