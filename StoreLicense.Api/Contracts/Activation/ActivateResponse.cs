
using StoreLicense.Core.Enums;

namespace StoreLicense.Api.Contracts.Activation;

public class ActivateResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid SubscriberId { get; set; }

    public Guid SubscriptionId { get; set; }

    public Guid DeviceId { get; set; }

    public SubscriptionStatus SubscriptionStatus { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsLifetime { get; set; }

    public int MaxDevices { get; set; }

    public int ActiveDevicesCount { get; set; }

    public DateTime ActivatedAt { get; set; }
}
