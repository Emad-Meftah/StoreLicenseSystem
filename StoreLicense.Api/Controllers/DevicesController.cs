
using System.Net;
using Microsoft.AspNetCore.Mvc;
using StoreLicense.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace StoreLicense.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IWebHostEnvironment _environment;

    public DevicesController(
        IDeviceService deviceService,
        IWebHostEnvironment environment)
    {
        _deviceService = deviceService;
        _environment = environment;
    }

    // عرض أجهزة اشتراك معين
    [HttpGet("subscription/{subscriptionId:guid}")]
    public async Task<IActionResult> GetBySubscription(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        var devices = await _deviceService.GetBySubscriptionAsync(
            subscriptionId,
            cancellationToken);

        var response = devices.Select(device => new
        {
            device.Id,
            device.SubscriberId,
            device.SubscriptionId,
            device.DeviceName,
            device.DeviceIdentifier,
            device.Status,
            StatusName = device.Status.ToString(),
            device.ActivatedAt,
            device.LastSeenAt
        });

        return Ok(response);
    }

    // إلغاء تفعيل جهاز
    [HttpPatch("{deviceId:guid}/revoke")]
    public async Task<IActionResult> Revoke(
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var result = await _deviceService.RevokeAsync(
            deviceId,
            cancellationToken);

        if (!result)
        {
            return NotFound(new
            {
                success = false,
                message = "الجهاز غير موجود."
            });
        }

        return Ok(new
        {
            success = true,
            message = "تم إلغاء تفعيل الجهاز بنجاح."
        });
    }

    // حذف سجل جهاز للاختبار - Development فقط
    [HttpDelete("{deviceId:guid}/test-delete")]
    public async Task<IActionResult> DeleteForTesting(
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress;

        if (remoteIp is null ||
            !IPAddress.IsLoopback(remoteIp))
        {
            return NotFound();
        }

        var result = await _deviceService.DeleteAsync(
            deviceId,
            cancellationToken);

        if (!result)
        {
            return NotFound(new
            {
                success = false,
                message = "سجل الجهاز غير موجود."
            });
        }

        return Ok(new
        {
            success = true,
            message = "تم حذف سجل الجهاز الاختباري بنجاح."
        });
    }
}
