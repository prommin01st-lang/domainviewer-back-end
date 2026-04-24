using Microsoft.AspNetCore.Mvc;

namespace DomainViewer.API.Common;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult ApiOk<T>(T data, string? message = null)
        => Ok(ApiResponse<T>.Ok(data, message));

    protected IActionResult ApiOk(string? message = null)
        => Ok(ApiResponse.Ok(message));

    protected IActionResult ApiBadRequest(string message, string? errorCode = null)
        => BadRequest(ApiResponse.Fail(message, errorCode ?? ErrorCodes.ValidationFailed));

    protected IActionResult ApiNotFound(string message = "ไม่พบข้อมูล")
        => NotFound(ApiResponse.Fail(message, ErrorCodes.NotFound));

    protected IActionResult ApiUnauthorized(string message = "ไม่ได้รับอนุญาต")
        => Unauthorized(ApiResponse.Fail(message, ErrorCodes.Unauthorized));

    protected IActionResult ApiUnauthorized(string message, string errorCode)
        => Unauthorized(ApiResponse.Fail(message, errorCode));

    protected IActionResult ApiForbidden(string message = "ไม่มีสิทธิ์เข้าถึง")
        => StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(message, ErrorCodes.Forbidden));

    protected IActionResult ApiConflict(string message, string? errorCode = null)
        => Conflict(ApiResponse.Fail(message, errorCode ?? ErrorCodes.Conflict));

    protected IActionResult ApiInternalError(string message = "เกิดข้อผิดพลาดภายในระบบ")
        => StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Fail(message, ErrorCodes.InternalError));
}
