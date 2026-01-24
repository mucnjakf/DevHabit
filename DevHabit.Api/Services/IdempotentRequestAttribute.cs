using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace DevHabit.Api.Services;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentRequestAttribute : Attribute, IAsyncActionFilter
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    public static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(60);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers
                .TryGetValue(IdempotencyKeyHeader, out StringValues idempotenceKeyValue) ||
            !Guid.TryParse(idempotenceKeyValue, out Guid idempotencyKey))
        {
            ProblemDetailsFactory problemDetailsFactory = context.HttpContext.RequestServices
                .GetRequiredService<ProblemDetailsFactory>();

            ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(context.HttpContext,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request", detail: $"Invalid or missing {IdempotencyKeyHeader} header");

            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }

        IMemoryCache cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
        string cacheKey = $"idempotence:{idempotencyKey}";

        int? statusCode = cache.Get<int?>(cacheKey);

        if (statusCode is not null)
        {
            context.Result = new StatusCodeResult(statusCode.Value);
            return;
        }

        ActionExecutedContext actionExecutedContext = await next();

        if (actionExecutedContext.Result is ObjectResult objectResult)
        {
            cache.Set(cacheKey, objectResult.StatusCode, DefaultCacheDuration);
        }
    }
}
