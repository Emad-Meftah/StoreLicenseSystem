using Microsoft.AspNetCore.Mvc;
using StoreLicense.Api.Contracts.Subscribers;
using StoreLicense.Core.Entities;
using StoreLicense.Core.Enums;
using StoreLicense.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace StoreLicense.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/subscribers")]
public class SubscribersController : ControllerBase
{
    private readonly ISubscriberService _subscriberService;

    public SubscribersController(ISubscriberService subscriberService)
    {
        _subscriberService = subscriberService;
    }

    // GET: api/subscribers
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriberResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var subscribers = await _subscriberService.GetAllAsync(
            cancellationToken);

        return Ok(subscribers.Select(MapToResponse).ToList());
    }

    // GET: api/subscribers/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubscriberResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subscriber = await _subscriberService.GetByIdAsync(
            id,
            cancellationToken);

        if (subscriber is null)
            return NotFound();

        return Ok(MapToResponse(subscriber));
    }

    // POST: api/subscribers
    [HttpPost]
    public async Task<ActionResult<SubscriberResponse>> Create(
        [FromBody] CreateSubscriberRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscriber = await _subscriberService.CreateAsync(
                request.StoreName,
                request.OwnerName,
                request.Phone,
                request.Notes,
                cancellationToken);

            var response = MapToResponse(subscriber);

            return CreatedAtAction(
                nameof(GetById),
                new { id = subscriber.Id },
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/subscribers/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubscriberResponse>> Update(
        Guid id,
        [FromBody] UpdateSubscriberRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscriber = await _subscriberService.UpdateAsync(
                id,
                request.StoreName,
                request.OwnerName,
                request.Phone,
                request.Notes,
                cancellationToken);

            if (subscriber is null)
                return NotFound();

            return Ok(MapToResponse(subscriber));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PATCH: api/subscribers/{id}/activate
    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _subscriberService.SetStatusAsync(
            id,
            SubscriberStatus.Active,
            cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }

    // PATCH: api/subscribers/{id}/deactivate
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _subscriberService.SetStatusAsync(
            id,
            SubscriberStatus.Suspended,
            cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }

    private static SubscriberResponse MapToResponse(
        Subscriber subscriber)
    {
        return new SubscriberResponse
        {
            Id = subscriber.Id,
            StoreName = subscriber.StoreName,
            OwnerName = subscriber.OwnerName,
            Phone = subscriber.Phone,
            Notes = subscriber.Notes,
            Status = subscriber.Status,
            CreatedAt = subscriber.CreatedAt,
            UpdatedAt = subscriber.UpdatedAt
        };
    }
}