using Core.DTOs;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baby_Clinic.Controllers;

[Route("api/tokens")]
[AllowAnonymous]
public class TokensController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public TokensController(IAuthService authService)
    {
        _authService = authService;
    }

    // התחברות: מחזירה JWT
    [HttpPost]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        return FromResult(result, Ok);
    }
}
