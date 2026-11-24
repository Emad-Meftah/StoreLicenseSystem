
using Microsoft.AspNetCore.Mvc;
using StoreLicense.Api.Contracts.Subscriptions;
using StoreLicense.Core.Entities;
using StoreLicense.Core.Enums;
using StoreLicense.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using StoreLicense.Infrastructure.Security;

namespace StoreLicense.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ActivationCodeProtectionService _activationCodeProtection;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ActivationCodeProtectionService activationCodeProtection)
    {
        _subscriptionService = subscriptionService;
        _activationCodeProtection = activationCodeProtection;
    }

    // GET: api/subscriptions
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var subscriptions = await _subscriptionService.GetAllAsync(
            cancellationToken);

        var response = subscriptions
            .Select(MapToListItem)
            .ToList();

        return Ok(response);
    }

    // GET: api/subscriptions/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.GetByIdAsync(
            id,
            cancellationToken);

        if (subscription is null)
        {
            return NotFound(new
            {
                message = "الاشتراك غير موجود."
            });
        }

        var response = MapToDetails(subscription);

        return Ok(response);
    }

    // POST: api/subscriptions
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _subscriptionService.CreateAsync(
                request.SubscriberId,
                request.PlanId,
                request.MaxDevices,
                request.ActivationCodeExpiresAt,
                cancellationToken);

            if (result is null)
            {
                return BadRequest(new
                {
                    message = "تعذر إنشاء الاشتراك."
                });
            }

            var subscription = result.Value.Subscription;

            var response = new CreateSubscriptionResponse
            {
                SubscriptionId = subscription.Id,
                SubscriberId = subscription.SubscriberId,
                StoreName = subscription.Subscriber.StoreName,

                PlanId = subscription.PlanId ?? Guid.Empty,
                PlanName = subscription.Plan!.Name,

                IsLifetime = subscription.IsLifetime,
                DurationDays = subscription.DurationDays,
                MaxDevices = subscription.MaxDevices,

                Status = subscription.Status,

                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,

                ActivationCode = result.Value.ActivationCode,
                ActivationCodeExpiresAt =
                    subscription.ActivationCodes.FirstOrDefault()?.ExpiresAt,

                CreatedAt = subscription.CreatedAt
            };

            return StatusCode(201, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // POST: api/subscriptions/{subscriptionId}/activation-codes
    [HttpPost("{subscriptionId:guid}/activation-codes")]
    public async Task<IActionResult> GenerateActivationCode(
        Guid subscriptionId,
        [FromBody] GenerateActivationCodeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _subscriptionService.GenerateActivationCodeAsync(
                subscriptionId,
                request.ExpiresAt,
                cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    message = "الاشتراك غير موجود."
                });
            }

            var response = new GenerateActivationCodeResponse
            {
                SubscriptionId = result.SubscriptionId,
                ActivationCode = result.ActivationCode,
                CreatedAt = result.CreatedAt,
                ExpiresAt = result.ExpiresAt
            };

            return StatusCode(201, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // تحويل الاشتراك إلى عنصر في القائمة
    private static SubscriptionListItemResponse MapToListItem(
        Subscription subscription)
    {
        return new SubscriptionListItemResponse
        {
            SubscriptionId = subscription.Id,

            SubscriberId = subscription.SubscriberId,
            StoreName = subscription.Subscriber.StoreName,
            OwnerName = subscription.Subscriber.OwnerName,

            PlanId = subscription.PlanId,
            PlanName = subscription.Plan?.Name,

            IsLifetime = subscription.IsLifetime,
            DurationDays = subscription.DurationDays,
            MaxDevices = subscription.MaxDevices,

            RegisteredDevicesCount = subscription.Devices.Count,
            ActiveDevicesCount = subscription.Devices.Count(
                x => x.Status == DeviceStatus.Active),

            Status = subscription.Status,

            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,

            CreatedAt = subscription.CreatedAt
        };
    }

    // تحويل الاشتراك إلى نموذج التفاصيل
    private SubscriptionDetailsResponse MapToDetails(
        Subscription subscription)
    {
        return new SubscriptionDetailsResponse
        {
            SubscriptionId = subscription.Id,

            SubscriberId = subscription.SubscriberId,
            StoreName = subscription.Subscriber.StoreName,
            OwnerName = subscription.Subscriber.OwnerName,
            Phone = subscription.Subscriber.Phone,

            PlanId = subscription.PlanId,
            PlanName = subscription.Plan?.Name,

            IsLifetime = subscription.IsLifetime,
            DurationDays = subscription.DurationDays,
            MaxDevices = subscription.MaxDevices,

            Status = subscription.Status,

            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,

            CreatedAt = subscription.CreatedAt,

            RegisteredDevicesCount = subscription.Devices.Count,
            ActiveDevicesCount = subscription.Devices.Count(
                x => x.Status == DeviceStatus.Active),

            Devices = subscription.Devices
                .Select(device => new SubscriptionDeviceResponse
                {
                    DeviceId = device.Id,
                    DeviceName = device.DeviceName,
                    ActivatedAt = device.ActivatedAt,
                    LastSeenAt = device.LastSeenAt,
                    Status = device.Status
                })
                .ToList(),

            ActivationCodes = subscription.ActivationCodes
                .Select(code => new SubscriptionActivationCodeResponse
                {
                    ActivationCodeId = code.Id,

                    ActivationCode = string.IsNullOrWhiteSpace(code.EncryptedCode)
                        ? null
                        : _activationCodeProtection.Unprotect(code.EncryptedCode),

                    Status = code.Status,
                    CreatedAt = code.CreatedAt,
                    ExpiresAt = code.ExpiresAt,
                    RedeemedAt = code.RedeemedAt
                })
                .ToList()
        };
    }

    // POST: api/subscriptions/{subscriptionId}/cancel
    [HttpPost("{subscriptionId:guid}/cancel")]
    public async Task<IActionResult> CancelSubscription(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        // السماح بالتنفيذ في بيئة التطوير فقط مؤقتًا.
        if (!HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment())
        {
            return NotFound();
        }

        // السماح بالطلبات المحلية فقط.
        var remoteIp = HttpContext.Connection.RemoteIpAddress;

        if (remoteIp is null ||
            !System.Net.IPAddress.IsLoopback(remoteIp))
        {
            return Forbid();
        }

        try
        {
            var cancelled = await _subscriptionService.CancelAsync(
                subscriptionId,
                cancellationToken);

            if (!cancelled)
            {
                return NotFound(new
                {
                    message = "الاشتراك غير موجود."
                });
            }

            return Ok(new
            {
                message = "تم إلغاء الاشتراك بنجاح.",
                subscriptionId
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
