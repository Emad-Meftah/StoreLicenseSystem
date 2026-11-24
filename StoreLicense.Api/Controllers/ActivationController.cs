
using Microsoft.AspNetCore.Mvc;
using StoreLicense.Api.Contracts.Activation;
using StoreLicense.Core.Interfaces;
using StoreLicense.Core.Models.Activation;

namespace StoreLicense.Api.Controllers;

[ApiController]
[Route("api/activation")]
public class ActivationController : ControllerBase
{
    private readonly IActivationService _activationService;

    public ActivationController(
        IActivationService activationService)
    {
        _activationService = activationService;
    }

    [HttpPost("activate")]
    public async Task<IActionResult> Activate(
        [FromBody] ActivateRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ActivateCommand
        {
            ActivationCode = request.ActivationCode,
            DeviceIdentifier = request.DeviceIdentifier,
            DeviceName = request.DeviceName,
            DevicePublicKey = request.DevicePublicKey
        };

        var ipAddress = HttpContext.Connection.RemoteIpAddress?
            .ToString();

        var result = await _activationService.ActivateAsync(
            command,
            ipAddress,
            cancellationToken);

        var response = new ActivateResponse
        {
            Success = result.Success,
            Message = result.Message,

            SubscriberId = result.SubscriberId,
            SubscriptionId = result.SubscriptionId,
            DeviceId = result.DeviceId,

            SubscriptionStatus = result.SubscriptionStatus,

            StartDate = result.StartDate,
            EndDate = result.EndDate,

            IsLifetime = result.IsLifetime,
            MaxDevices = result.MaxDevices,
            ActiveDevicesCount = result.ActiveDevicesCount,

            ActivatedAt = result.ActivatedAt
        };

        if (!result.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
