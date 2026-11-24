
using StoreLicense.Core.Enums;

namespace StoreLicense.Api.Contracts.Subscriptions;

public class SubscriptionListItemResponse
{
    public Guid SubscriptionId { get; set; }

    public Guid SubscriberId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public Guid? PlanId { get; set; }

    public string? PlanName { get; set; }

    public bool IsLifetime { get; set; }

    public int? DurationDays { get; set; }

    public int MaxDevices { get; set; }

    public int RegisteredDevicesCount { get; set; }

    public int ActiveDevicesCount { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
