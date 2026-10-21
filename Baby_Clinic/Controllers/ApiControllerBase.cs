using System.Security.Claims;
using Core.Common;
using Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baby_Clinic.Controllers;

// כל Controller דורש token כברירת מחדל; הרשמה והתחברות מסומנות AllowAnonymous
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    // מזהה המשתמש וה-role נלקחים מה-claims של ה-token, לא מגוף הבקשה
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected Role CurrentRole => Enum.Parse<Role>(User.FindFirstValue(ClaimTypes.Role)!);

    // המקום היחיד שהופך תוצאה של שירות לסטטוס HTTP
    protected IActionResult FromResult(Result result, Func<IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess();

        var statusCode = result.Status switch
        {
            ResultStatus.Validation => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict, // שלב 7: משבצת שנתפסה -> 409
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(detail: result.Message, statusCode: statusCode);
    }

    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess) =>
        FromResult((Result)result, () => onSuccess(result.Value!));
}
