using StoreLicense.Core.Models.LicenseValidation;

namespace StoreLicense.Core.Interfaces;

public interface ILicenseValidationService
{
    Task<LicenseValidationResult> ValidateAsync(
        ValidateLicenseCommand command,
        CancellationToken cancellationToken = default);
}