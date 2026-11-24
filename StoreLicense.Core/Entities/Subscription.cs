using StoreLicense.Core.Enums;

namespace StoreLicense.Core.Entities;

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SubscriberId { get; set; }

    public Guid? PlanId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsLifetime { get; set; }

    public int? DurationDays { get; set; }

    public int MaxDevices { get; set; } = 1;

    public SubscriptionStatus Status { get; set; } =
        SubscriptionStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Subscriber Subscriber { get; set; } = null!;

    public SubscriptionPlan? Plan { get; set; }

    public ICollection<ActivationCode> ActivationCodes { get; set; }
        = new List<ActivationCode>();

    public ICollection<SubscriberDevice> Devices { get; set; }
        = new List<SubscriberDevice>();

    public ICollection<SubscriptionRenewal> Renewals { get; set; }
        = new List<SubscriptionRenewal>();

    public ICollection<LicenseAuditLog> AuditLogs { get; set; }
    = new List<LicenseAuditLog>();
}