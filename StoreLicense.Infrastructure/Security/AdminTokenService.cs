
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StoreLicense.Core.Entities;

namespace StoreLicense.Infrastructure.Security;

public class AdminTokenService
{
    private readonly IConfiguration _configuration;

    public AdminTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(AdminUser admin)
    {
        var secretKey = _configuration["AdminJwt:SecretKey"];
        var issuer = _configuration["AdminJwt:Issuer"];
        var audience = _configuration["AdminJwt:Audience"];

        if (string.IsNullOrWhiteSpace(secretKey) ||
            string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "Admin JWT configuration is incomplete.");
        }

        if (!int.TryParse(
                _configuration["AdminJwt:AccessTokenMinutes"],
                out var expiryMinutes) ||
            expiryMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Admin JWT expiration is invalid.");
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, admin.Email),
            new(JwtRegisteredClaimNames.Name, admin.FullName),
            new(ClaimTypes.Role, "Admin"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
