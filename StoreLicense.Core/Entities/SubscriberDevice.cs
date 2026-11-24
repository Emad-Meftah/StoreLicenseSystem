using StoreLicense.Core.Enums;

namespace StoreLicense.Core.Entities;

public class SubscriberDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SubscriberId { get; set; }

    public Guid SubscriptionId { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string DevicePublicKey { get; set; } = string.Empty;

    public string DeviceIdentifier { get; set; } = string.Empty;

    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastSeenAt { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Active;

    public Subscriber Subscriber { get; set; } = null!;

    public Subscription Subscription { get; set; } = null!;
}