using System.Net;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Common.Exceptions;
using ValidationException = RestaurantSaaS.Application.Common.Exceptions.ValidationException;

namespace RestaurantSaaS.Api.Middleware;

/// <summary>Maps Application/Domain exceptions to RFC 7807 problem-details responses so callers get a
/// consistent error shape regardless of which layer threw.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "One or more validation errors occurred."),
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found."),
            ForbiddenAccessException => (HttpStatusCode.Forbidden, "Access denied."),
            ConflictException => (HttpStatusCode.Conflict, "Conflict."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized."),
            Domain.Exceptions.SubscriptionLockedException => (HttpStatusCode.PaymentRequired, "Subscription locked."),
            Domain.Exceptions.DomainException => (HttpStatusCode.BadRequest, "Business rule violation."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred."),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "{StatusCode} handling {Method} {Path}: {Message}", (int)statusCode, context.Request.Method, context.Request.Path, exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = statusCode == HttpStatusCode.InternalServerError ? "An unexpected error occurred. Please contact support." : exception.Message,
            Instance = context.Request.Path,
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        problemDetails.Extensions["correlationId"] = context.Response.Headers["X-Correlation-Id"].ToString();

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
