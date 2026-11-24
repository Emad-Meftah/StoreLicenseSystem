
using StoreLicense.Core.Enums;

namespace StoreLicense.Api.Contracts.Subscriptions;

public class SubscriptionDetailsResponse
{
    public Guid SubscriptionId { get; set; }

    public Guid SubscriberId { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public Guid? PlanId { get; set; }

    public string? PlanName { get; set; }

    public bool IsLifetime { get; set; }

    public int? DurationDays { get; set; }

    public int MaxDevices { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public int RegisteredDevicesCount { get; set; }

    public int ActiveDevicesCount { get; set; }

    public List<SubscriptionDeviceResponse> Devices { get; set; } = new();

    public List<SubscriptionActivationCodeResponse> ActivationCodes { get; set; } = new();
}

public class SubscriptionDeviceResponse
{
    public Guid DeviceId { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public DateTime ActivatedAt { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public DeviceStatus Status { get; set; }
}

public class SubscriptionActivationCodeResponse
{
    public Guid ActivationCodeId { get; set; }

    public string? ActivationCode { get; set; }

    public ActivationCodeStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RedeemedAt { get; set; }
}
