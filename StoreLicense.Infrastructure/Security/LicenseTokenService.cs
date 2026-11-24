
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using StoreLicense.Core.Interfaces;
using StoreLicense.Core.Models.LicenseValidation;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace StoreLicense.Infrastructure.Security;

public class LicenseTokenService : ILicenseTokenService
{
    private readonly ECDsaSecurityKey _securityKey;
    private readonly SigningCredentials _signingCredentials;

    public LicenseTokenService(IConfiguration configuration)
    {

        // قراءة المفتاح الخاص من Base64 أولاً،
        // ثم استخدام الملف المحلي عند عدم وجوده.
        string privateKeyPem;

        var privateKeyBase64 = configuration[
            "LicenseSigning:PrivateKeyPemBase64"];

        if (!string.IsNullOrWhiteSpace(privateKeyBase64))
        {
            try
            {
                var privateKeyBytes =
                    Convert.FromBase64String(privateKeyBase64);

                privateKeyPem = Encoding.UTF8.GetString(privateKeyBytes);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "License signing private key Base64 is invalid.", ex);
            }
        }
        else
        {
            var privateKeyPath = configuration[
                "LicenseSigning:PrivateKeyPath"];

            if (string.IsNullOrWhiteSpace(privateKeyPath))
            {
                throw new InvalidOperationException(
                    "License signing private key is not configured.");
            }

            if (!File.Exists(privateKeyPath))
            {
                throw new FileNotFoundException(
                    "License signing private key file was not found.",
                    privateKeyPath);
            }

            privateKeyPem = File.ReadAllText(privateKeyPath);
        }

        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);

        _securityKey = new ECDsaSecurityKey(ecdsa)
        {
            KeyId = "license-signing-key-v1"
        };

        _signingCredentials = new SigningCredentials(
            _securityKey,
            SecurityAlgorithms.EcdsaSha256);
    }

    public string GenerateToken(LicenseTokenPayload payload)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("license_version", payload.LicenseVersion.ToString()),
            new("subscriber_id", payload.SubscriberId.ToString()),
            new("subscription_id", payload.SubscriptionId.ToString()),
            new("device_id", payload.DeviceId.ToString()),
            new("is_lifetime", payload.IsLifetime.ToString().ToLowerInvariant()),
            new("max_devices", payload.MaxDevices.ToString()),
            new("grace_period_days", payload.GracePeriodDays.ToString())
        };

        var now = new DateTimeOffset(payload.IssuedAt).ToUnixTimeSeconds();

        var expiresAt = new DateTimeOffset(payload.ExpiresAt)
            .ToUnixTimeSeconds();

        var nextValidationAt = new DateTimeOffset(payload.NextValidationAt)
            .ToUnixTimeSeconds();

        claims.Add(new("iat", now.ToString(),
            System.Security.Claims.ClaimValueTypes.Integer64));

        claims.Add(new("exp", expiresAt.ToString(),
            System.Security.Claims.ClaimValueTypes.Integer64));

        claims.Add(new("next_validation", nextValidationAt.ToString(),
            System.Security.Claims.ClaimValueTypes.Integer64));

        if (payload.SubscriptionStartDate.HasValue)
        {
            claims.Add(new(
                "subscription_start",
                new DateTimeOffset(payload.SubscriptionStartDate.Value)
                    .ToUnixTimeSeconds().ToString(),
                System.Security.Claims.ClaimValueTypes.Integer64));
        }

        if (payload.SubscriptionEndDate.HasValue)
        {
            claims.Add(new(
                "subscription_end",
                new DateTimeOffset(payload.SubscriptionEndDate.Value)
                    .ToUnixTimeSeconds().ToString(),
                System.Security.Claims.ClaimValueTypes.Integer64));
        }

        var token = new JwtSecurityToken(
            claims: claims,
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
