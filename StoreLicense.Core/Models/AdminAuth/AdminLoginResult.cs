
namespace StoreLicense.Core.Models.AdminAuth;

public class AdminLoginResult
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public int ExpiresInSeconds { get; set; }

    public Guid AdminId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}
