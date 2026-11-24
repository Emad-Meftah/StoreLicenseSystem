
using Microsoft.AspNetCore.Mvc;
using StoreLicense.Api.Contracts.SubscriptionPlans;
using StoreLicense.Core.Entities;
using StoreLicense.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace StoreLicense.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/subscription-plans")]
public class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _service;

    public SubscriptionPlansController(
        ISubscriptionPlanService service)
    {
        _service = service;
    }

    // GET: api/subscription-plans
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanResponse>>>
        GetAll(CancellationToken cancellationToken)
    {
        var plans = await _service.GetAllAsync(cancellationToken);

        return Ok(plans.Select(MapToResponse).ToList());
    }

    // GET: api/subscription-plans/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubscriptionPlanResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var plan = await _service.GetByIdAsync(id, cancellationToken);

        if (plan is null)
        {
            return NotFound(new { message = "خطة الاشتراك غير موجودة." });
        }

        return Ok(MapToResponse(plan));
    }

    // POST: api/subscription-plans
    [HttpPost]
    public async Task<ActionResult<SubscriptionPlanResponse>> Create(
        [FromBody] CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _service.CreateAsync(
                request.Name,
                request.DurationDays,
                request.IsLifetime,
                request.DefaultMaxDevices,
                cancellationToken
            );

            var response = MapToResponse(plan);

            return CreatedAtAction(
                nameof(GetById),
                new { id = plan.Id },
                response
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT: api/subscription-plans/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubscriptionPlanResponse>> Update(
        Guid id,
        [FromBody] UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _service.UpdateAsync(
                id,
                request.Name,
                request.DurationDays,
                request.IsLifetime,
                request.DefaultMaxDevices,
                cancellationToken
            );

            if (plan is null)
            {
                return NotFound(new { message = "خطة الاشتراك غير موجودة." });
            }

            return Ok(MapToResponse(plan));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PATCH: api/subscription-plans/{id}/activate
    [HttpPatch("{id:guid}/activate")]
    public Task<ActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeStatus(id, true, cancellationToken);
    }

    // PATCH: api/subscription-plans/{id}/deactivate
    [HttpPatch("{id:guid}/deactivate")]
    public Task<ActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        return ChangeStatus(id, false, cancellationToken);
    }

    private async Task<ActionResult> ChangeStatus(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var updated = await _service.SetActiveStatusAsync(
            id,
            isActive,
            cancellationToken
        );

        if (!updated)
        {
            return NotFound(new { message = "خطة الاشتراك غير موجودة." });
        }

        return NoContent();
    }

    private static SubscriptionPlanResponse MapToResponse(
        SubscriptionPlan plan)
    {
        return new SubscriptionPlanResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            DurationDays = plan.DurationDays,
            IsLifetime = plan.IsLifetime,
            DefaultMaxDevices = plan.DefaultMaxDevices,
            IsActive = plan.IsActive,
            CreatedAt = plan.CreatedAt
        };
    }
}
