using Core.Common;
using Core.DTOs;
using Core.Enums;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baby_Clinic.Controllers;

[Route("api/slots")]
public class SlotsController : ApiControllerBase
{
    private readonly ISlotService _slotService;

    public SlotsController(ISlotService slotService)
    {
        _slotService = slotService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SlotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search([FromQuery] SlotSearchParams search, CancellationToken ct)
    {
        var result = await _slotService.SearchAsync(search, ct);
        return FromResult(result, Ok);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var result = await _slotService.GetByIdAsync(id, ct);
        return FromResult(result, Ok);
    }

    [HttpPost]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateSlotRequest request, CancellationToken ct)
    {
        var result = await _slotService.CreateAsync(request, ct);
        return FromResult(result, slot => CreatedAtAction(nameof(GetById), new { id = slot.Id }, slot));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] CreateSlotRequest request, CancellationToken ct)
    {
        var result = await _slotService.UpdateAsync(id, request, ct);
        return FromResult(result, NoContent);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(Role.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var result = await _slotService.DeleteAsync(id, ct);
        return FromResult(result, NoContent);
    }
}
