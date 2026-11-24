using Microsoft.AspNetCore.DataProtection;

namespace StoreLicense.Infrastructure.Security;

public sealed class ActivationCodeProtectionService
{
    private readonly IDataProtector _protector;

    public ActivationCodeProtectionService(
        IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(
            "StoreLicenseSystem.ActivationCode.v1");
    }

    public string Protect(string activationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activationCode);

        return _protector.Protect(activationCode);
    }

    public string Unprotect(string protectedActivationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedActivationCode);

        return _protector.Unprotect(protectedActivationCode);
    }
}
