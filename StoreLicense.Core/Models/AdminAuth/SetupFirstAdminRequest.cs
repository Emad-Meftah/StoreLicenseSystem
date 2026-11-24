
using System.ComponentModel.DataAnnotations;

namespace StoreLicense.Core.Models.AdminAuth;

public class SetupFirstAdminRequest
{
    [Required]
    public string SetupKey { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MinLength(12)]
    public string Password { get; set; } = string.Empty;
}
