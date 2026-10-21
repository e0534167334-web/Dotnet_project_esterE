using Core.DTOs;
using Core.Enums;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baby_Clinic.Controllers;

[Route("api/appointments")]
public class AppointmentsController : ApiControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpPost]
    [Authorize(Roles = nameof(Role.Parent))]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    // שלב 7: 409 מתועד ב-Swagger - המשבצת נתפסה ע"י הורה אחר
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book([FromBody] BookAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointmentService.BookAsync(CurrentUserId, request, ct);

        // אין endpoint לתור בודד, ולכן ה-Location מפנה לרשימת התורים
        return FromResult(result, appointment => CreatedAtAction(nameof(GetAll), null, appointment));
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateAppointmentRequest request, CancellationToken ct)
    {
        // השינוי היחיד שנתמך כרגע הוא ביטול
        if (request.Status != AppointmentStatus.Cancelled)
        {
            ModelState.AddModelError(nameof(request.Status), "Only 'Cancelled' is supported.");
            return ValidationProblem(ModelState);
        }

        var result = await _appointmentService.CancelAsync(id, CurrentUserId, CurrentRole, ct);
        return FromResult(result, NoContent);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _appointmentService.GetAsync(CurrentUserId, CurrentRole, ct);
        return FromResult(result, Ok);
    }
}
