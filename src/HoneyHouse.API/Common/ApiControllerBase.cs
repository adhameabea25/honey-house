using HoneyHouse.Application.Common;
using HoneyHouse.Application.Localization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.API.Common;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ILocalizer? _localizer;
    protected ILocalizer L => _localizer ??= HttpContext.RequestServices.GetRequiredService<ILocalizer>();

    protected IActionResult FromResult(Result result) =>
        result.IsSuccess ? NoContent() : Problem(result.Errors);

    protected IActionResult FromResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Problem(result.Errors);

    protected IActionResult CreatedFromResult<T>(Result<T> result, string actionName, Func<T, object> routeValues) =>
        result.IsSuccess ? CreatedAtAction(actionName, routeValues(result.Value), result.Value) : Problem(result.Errors);

    protected IActionResult Problem(IReadOnlyList<Error> errors)
    {
        var first = errors[0];
        var status = first.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status422UnprocessableEntity
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = first.Type == ErrorType.Validation ? L["validation_failed"] : L[first.Code, first.Args ?? []],
        };
        problem.Extensions["code"] = first.Type == ErrorType.Validation && errors.Count > 1 ? "validation_failed" : first.Code;

        if (first.Type == ErrorType.Validation)
        {
            problem.Extensions["errors"] = errors
                .GroupBy(e => e.Field ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.Select(e => L[e.Code, e.Args ?? []]).ToArray());
        }

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
