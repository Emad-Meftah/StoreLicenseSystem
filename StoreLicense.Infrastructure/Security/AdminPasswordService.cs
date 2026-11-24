
using Microsoft.AspNetCore.Identity;
using StoreLicense.Core.Entities;

namespace StoreLicense.Infrastructure.Security;

public class AdminPasswordService
{
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    public string HashPassword(AdminUser admin, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException(
                "Password cannot be empty.",
                nameof(password));

        return _passwordHasher.HashPassword(admin, password);
    }

    public bool VerifyPassword(AdminUser admin, string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        var result = _passwordHasher.VerifyHashedPassword(
            admin,
            admin.PasswordHash,
            password);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
