using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using ValidationException = RestaurantSaaS.Application.Common.Exceptions.ValidationException;

namespace RestaurantSaaS.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} for user {UserId}", requestName, currentUser.UserId);
        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName}", requestName);
            return response;
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            logger.LogError(ex, "Unhandled exception while processing {RequestName}", requestName);
            throw;
        }
    }
}

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0) throw new ValidationException(failures);

        return await next();
    }
}

/// <summary>Defensive, tenant-scoped commands/queries implement <see cref="ITenantScopedRequest"/>; this
/// behavior rejects any request whose declared TenantId doesn't match the caller's JWT tenant claim,
/// as a second line of defense alongside the EF Core global query filter.</summary>
public interface ITenantScopedRequest
{
    Guid TenantId { get; }
}

public sealed class TenantAuthorizationBehavior<TRequest, TResponse>(ITenantProvider tenantProvider)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is ITenantScopedRequest scoped && !tenantProvider.IsSuperAdmin)
        {
            if (tenantProvider.TenantId is null || tenantProvider.TenantId != scoped.TenantId)
                throw new ForbiddenAccessException("Request targets a tenant other than the caller's own.");
        }
        return next();
    }
}
