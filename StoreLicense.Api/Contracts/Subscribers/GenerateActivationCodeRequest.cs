
using System.ComponentModel.DataAnnotations;

namespace StoreLicense.Api.Contracts.Subscriptions;

public class GenerateActivationCodeRequest
{
    public DateTime? ExpiresAt { get; set; }
}
