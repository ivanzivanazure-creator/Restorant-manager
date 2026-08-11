using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RestaurantSaaS.Application.Auth;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Pos;

namespace RestaurantSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<TokenIssuer>();
        services.AddScoped<OrderKitchenDispatchService>();

        // Behavior order = execution order (outermost first): logging wraps everything (so it also
        // catches/logs validation and authorization failures), then validation, then tenant authorization,
        // then the handler itself.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TenantAuthorizationBehavior<,>));
        });

        return services;
    }
}
