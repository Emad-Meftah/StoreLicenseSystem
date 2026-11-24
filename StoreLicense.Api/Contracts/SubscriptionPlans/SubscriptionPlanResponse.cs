namespace StoreLicense.Api.Contracts.SubscriptionPlans;

public class SubscriptionPlanResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? DurationDays { get; set; }

    public bool IsLifetime { get; set; }

    public int DefaultMaxDevices { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}