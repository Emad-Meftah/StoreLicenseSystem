namespace StoreLicense.Core.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public int? DurationDays { get; set; }

    public bool IsLifetime { get; set; }

    public int DefaultMaxDevices { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Subscription> Subscriptions { get; set; }
        = new List<Subscription>();
}