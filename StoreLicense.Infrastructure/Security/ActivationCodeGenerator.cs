
using System.Security.Cryptography;
using System.Text;

namespace StoreLicense.Infrastructure.Security;

public static class ActivationCodeGenerator
{
    public static string Generate()
    {
        // توليد 32 بايت عشوائية آمنة
        var randomBytes = RandomNumberGenerator.GetBytes(32);

        // تحويلها إلى نص Base64 URL-safe
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public static string ComputeHash(string activationCode)
    {
        var codeBytes = Encoding.UTF8.GetBytes(activationCode);

        var hashBytes = SHA256.HashData(codeBytes);

        return Convert.ToHexString(hashBytes);
    }
}
