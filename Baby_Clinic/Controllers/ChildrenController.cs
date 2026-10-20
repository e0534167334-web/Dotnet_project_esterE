using Core.DTOs;
using Core.Enums;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baby_Clinic.Controllers;

[Route("api/children")]
[Authorize(Roles = nameof(Role.Parent))]
public class ChildrenController : ApiControllerBase
{
    private readonly IChildService _childService;

    public ChildrenController(IChildService childService)
    {
        _childService = childService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChildResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateChildRequest request, CancellationToken ct)
    {
        var result = await _childService.AddAsync(CurrentUserId, request, ct);

        // אין endpoint לילד בודד, ולכן ה-Location מפנה לרשימת הילדים של ההורה
        return FromResult(result, child => CreatedAtAction(nameof(GetMine), null, child));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ChildResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await _childService.GetByParentAsync(CurrentUserId, ct);
        return FromResult(result, Ok);
    }
}
