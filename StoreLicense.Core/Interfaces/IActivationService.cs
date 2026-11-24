
using StoreLicense.Core.Models.Activation;

namespace StoreLicense.Core.Interfaces;

public interface IActivationService
{
    Task<ActivationResult> ActivateAsync(
        ActivateCommand command,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
