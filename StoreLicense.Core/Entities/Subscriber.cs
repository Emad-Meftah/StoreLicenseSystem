using StoreLicense.Core.Enums;

namespace StoreLicense.Core.Entities;

public class Subscriber
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string StoreName { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public SubscriberStatus Status { get; set; } =
        SubscriberStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Subscription> Subscriptions { get; set; }
        = new List<Subscription>();

    public ICollection<SubscriberDevice> Devices { get; set; }
        = new List<SubscriberDevice>();

    // سجل عمليات المشترك
    public ICollection<LicenseAuditLog> AuditLogs { get; set; }
        = new List<LicenseAuditLog>();
}