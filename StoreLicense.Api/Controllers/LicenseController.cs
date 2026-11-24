using Microsoft.AspNetCore.Mvc;
using StoreLicense.Api.Contracts.LicenseValidation;
using StoreLicense.Core.Interfaces;
using StoreLicense.Core.Models.LicenseValidation;

namespace StoreLicense.Api.Controllers;

[ApiController]
[Route("api/license")]
public class LicenseController : ControllerBase
{
    private readonly ILicenseValidationService _licenseService;

    public LicenseController(
        ILicenseValidationService licenseService)
    {
        _licenseService = licenseService;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate(
        [FromBody] ValidateLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ValidateLicenseCommand
        {
            DeviceId = request.DeviceId,
            DeviceIdentifier = request.DeviceIdentifier,
            Timestamp = request.Timestamp,
            Nonce = request.Nonce,
            Signature = request.Signature
        };

        var result = await _licenseService.ValidateAsync(
            command,
            cancellationToken);

        var response = new ValidateLicenseResponse
        {
            Success = result.Success,
            Message = result.Message,
            LicenseToken = result.LicenseToken,
            ServerTime = result.ServerTime,
            LicenseExpiresAt = result.LicenseExpiresAt,
            NextValidationAt = result.NextValidationAt,
            GracePeriodDays = result.GracePeriodDays
        };

        if (!result.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}