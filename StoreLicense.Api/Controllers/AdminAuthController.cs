
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StoreLicense.Core.Entities;
using StoreLicense.Core.Models.AdminAuth;
using StoreLicense.Infrastructure.Data;
using StoreLicense.Infrastructure.Security;

namespace StoreLicense.Api.Controllers;

[ApiController]
[Route("api/admin-auth")]
public class AdminAuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly AdminPasswordService _passwordService;
    private readonly AdminTokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AdminAuthController(
        AppDbContext dbContext,
        AdminPasswordService passwordService,
        AdminTokenService tokenService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    // POST: api/admin-auth/setup-first-admin
    [HttpPost("setup-first-admin")]
    public async Task<ActionResult<AdminLoginResult>> SetupFirstAdmin(
        [FromBody] SetupFirstAdminRequest request)
    {
        var configuredKey = _configuration["AdminSetup:SetupKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return StatusCode(503, new
            {
                message = "Initial admin setup is not configured."
            });
        }

        var expectedKey = Encoding.UTF8.GetBytes(configuredKey);
        var providedKey = Encoding.UTF8.GetBytes(request.SetupKey);

        if (!CryptographicOperations.FixedTimeEquals(
                expectedKey,
                providedKey))
        {
            return Unauthorized(new
            {
                message = "Invalid setup key."
            });
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        await _dbContext.Database.ExecuteSqlRawAsync(
            "LOCK TABLE admin_users IN EXCLUSIVE MODE");

        var adminExists = await _dbContext.AdminUsers.AnyAsync();

        if (adminExists)
        {
            await transaction.RollbackAsync();

            return Conflict(new
            {
                message = "An administrator already exists. Initial setup is closed."
            });
        }

        var admin = new AdminUser
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            IsActive = true
        };

        admin.PasswordHash = _passwordService.HashPassword(
            admin,
            request.Password);

        _dbContext.AdminUsers.Add(admin);

        await _dbContext.SaveChangesAsync();

        var token = _tokenService.GenerateToken(admin);

        var result = new AdminLoginResult
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresInSeconds = GetAccessTokenLifetimeSeconds(),
            AdminId = admin.Id,
            Email = admin.Email,
            FullName = admin.FullName
        };

        await transaction.CommitAsync();

        return Ok(result);
    }

    // POST: api/admin-auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResult>> Login(
        [FromBody] AdminLoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var admin = await _dbContext.AdminUsers
            .FirstOrDefaultAsync(x => x.Email == email);

        if (admin == null ||
            !admin.IsActive ||
            !_passwordService.VerifyPassword(admin, request.Password))
        {
            return Unauthorized(new
            {
                message = "البريد الإلكتروني أو كلمة المرور غير صحيحة."
            });
        }

        var now = DateTime.UtcNow;

        admin.LastLoginAt = now;
        admin.UpdatedAt = now;

        await _dbContext.SaveChangesAsync();

        var token = _tokenService.GenerateToken(admin);

        var result = new AdminLoginResult
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresInSeconds = GetAccessTokenLifetimeSeconds(),
            AdminId = admin.Id,
            Email = admin.Email,
            FullName = admin.FullName
        };

        return Ok(result);
    }

    // POST: api/admin-auth/admins
    [Authorize]
    [HttpPost("admins")]
    public async Task<IActionResult> CreateAdmin(
        [FromBody] AddAdminRequest request)
    {
        // استخراج معرف المدير الحالي من JWT.
        var adminIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!Guid.TryParse(adminIdValue, out var currentAdminId))
        {
            return Unauthorized();
        }

        // التأكد من أن المدير الحالي ما زال نشطاً.
        var currentAdmin = await _dbContext.AdminUsers
            .FirstOrDefaultAsync(x => x.Id == currentAdminId);

        if (currentAdmin == null || !currentAdmin.IsActive)
        {
            return Unauthorized();
        }

        var email = request.Email.Trim().ToLowerInvariant();

        // التحقق من عدم تكرار البريد الإلكتروني.
        var emailExists = await _dbContext.AdminUsers
            .AnyAsync(x => x.Email == email);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "هذا البريد الإلكتروني مستخدم بالفعل."
            });
        }

        var newAdmin = new AdminUser
        {
            Email = email,
            FullName = request.FullName.Trim(),
            IsActive = true
        };

        newAdmin.PasswordHash = _passwordService.HashPassword(
            newAdmin,
            request.Password);

        _dbContext.AdminUsers.Add(newAdmin);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "تعذر إنشاء المدير. قد يكون البريد الإلكتروني مستخدماً بالفعل."
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            adminId = newAdmin.Id,
            email = newAdmin.Email,
            fullName = newAdmin.FullName,
            isActive = newAdmin.IsActive,
            createdAt = newAdmin.CreatedAt
        });
    }

    private int GetAccessTokenLifetimeSeconds()
    {
        if (!int.TryParse(
                _configuration["AdminJwt:AccessTokenMinutes"],
                out var minutes) ||
            minutes <= 0)
        {
            throw new InvalidOperationException(
                "Admin JWT expiration is invalid.");
        }

        return checked(minutes * 60);
    }
}
