using Core.DTOs;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Baby_Clinic.Controllers;

[Route("api/visit-types")]
public class VisitTypesController : ApiControllerBase
{
    private readonly ISlotService _slotService;

    public VisitTypesController(ISlotService slotService)
    {
        _slotService = slotService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<VisitTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _slotService.GetVisitTypesAsync(ct);
        return FromResult(result, Ok);
    }
}
