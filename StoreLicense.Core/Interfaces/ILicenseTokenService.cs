using StoreLicense.Core.Models.LicenseValidation;

namespace StoreLicense.Core.Interfaces;

public interface ILicenseTokenService
{
    string GenerateToken(LicenseTokenPayload payload);
}