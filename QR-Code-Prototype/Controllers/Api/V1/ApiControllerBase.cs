using Microsoft.AspNetCore.Mvc;
using QR_Code_Prototype.Contracts.Common;

namespace QR_Code_Prototype.Controllers.Api.V1;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult FromResult<T>(ApiResult<T> result)
    {
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        if (result.StatusCode == StatusCodes.Status204NoContent)
        {
            return NoContent();
        }

        return StatusCode(result.StatusCode, result.Value);
    }
}
