using StoreLicense.Core.Enums;

namespace StoreLicense.Api.Contracts.Subscribers;

public class SubscriberResponse
{
    public Guid Id { get; set; }

    public string StoreName { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public SubscriberStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}