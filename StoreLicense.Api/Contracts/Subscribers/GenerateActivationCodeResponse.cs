namespace StoreLicense.Api.Contracts.Subscriptions;

public class GenerateActivationCodeResponse
{
    public Guid SubscriptionId { get; set; }

    public string ActivationCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }
}