namespace StoreLicense.Core.Entities;

public class SubscriptionRenewal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SubscriptionId { get; set; }

    public DateTime? PreviousEndDate { get; set; }

    public DateTime? NewEndDate { get; set; }

    public DateTime RenewedAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    public Subscription Subscription { get; set; } = null!;
}