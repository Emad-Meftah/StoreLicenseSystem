namespace StoreLicense.Core.Models.Activation;

public class GeneratedActivationCode
{
    public Guid SubscriptionId { get; set; }

    public string ActivationCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }
}