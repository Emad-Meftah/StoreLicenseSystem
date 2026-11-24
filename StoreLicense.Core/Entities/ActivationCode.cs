using StoreLicense.Core.Enums;

namespace StoreLicense.Core.Entities;

public class ActivationCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SubscriptionId { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public string? EncryptedCode { get; set; }

    public ActivationCodeStatus Status { get; set; } =
        ActivationCodeStatus.Generated;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public DateTime? RedeemedAt { get; set; }

    public Subscription Subscription { get; set; } = null!;
}