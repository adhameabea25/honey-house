using FluentValidation;
using HoneyHouse.Application.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HoneyHouse.API.Middleware;

/// <summary>Runs FluentValidation validators for every action argument and returns a localized 400 on failure.</summary>
public sealed class ValidationFilter(IServiceProvider services, ILocalizer localizer) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>();

        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(arg), context.HttpContext.RequestAborted);
            foreach (var failure in result.Errors)
            {
                var key = ToCamelCase(failure.PropertyName);
                var message = Messages.Exists(failure.ErrorCode) ? localizer[failure.ErrorCode] : failure.ErrorMessage;
                if (!errors.TryGetValue(key, out var list)) errors[key] = list = [];
                if (!list.Contains(message)) list.Add(message);
            }
        }

        if (errors.Count > 0)
        {
            var problem = new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = localizer["validation_failed"] };
            problem.Extensions["code"] = "validation_failed";
            problem.Extensions["errors"] = errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
            context.Result = new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
            return;
        }

        await next();
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : string.Join('.', name.Split('.').Select(p => char.ToLowerInvariant(p[0]) + p[1..]));
}
