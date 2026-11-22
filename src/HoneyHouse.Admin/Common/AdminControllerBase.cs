using HoneyHouse.Admin.Localization;
using HoneyHouse.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Common;

[Authorize(Policy = AdminServiceExtensions.StaffPolicy)]
public abstract class AdminControllerBase : Controller
{
    public const string FlashSuccess = "flash_success";
    public const string FlashError = "flash_error";

    protected IText T => HttpContext.RequestServices.GetRequiredService<IText>();

    protected void Success(string key = "saved") => TempData[FlashSuccess] = T[key];
    protected void Failure(Error error) => TempData[FlashError] = Describe(error);

    /// <summary>Adds a service error to ModelState so the form is shown again with the message.</summary>
    protected void AddError(Error error, string field = "") => ModelState.AddModelError(field, Describe(error));

    /// <summary>Runs FluentValidation on a request and copies failures into ModelState (messages are error codes).</summary>
    protected async Task<bool> ValidateAsync<TRequest>(TRequest request, string prefix = "")
    {
        var validator = HttpContext.RequestServices.GetService<FluentValidation.IValidator<TRequest>>();
        if (validator is null) return ModelState.IsValid;
        var result = await validator.ValidateAsync(request);
        foreach (var failure in result.Errors)
        {
            var key = string.IsNullOrEmpty(prefix) ? failure.PropertyName : $"{prefix}.{failure.PropertyName}";
            ModelState.AddModelError(key, ValidationText.Describe(T, failure));
        }
        return ModelState.IsValid;
    }

    protected string Describe(Error error) => T[error.Code, error.Args ?? []];

    /// <summary>Shows service errors at the top of the form.</summary>
    protected void AddErrors(IReadOnlyList<Error> errors)
    {
        foreach (var e in errors) ModelState.AddModelError("", Describe(e));
    }
}
