using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace RestaurantSaaS.Infrastructure.Logging;

public static class SerilogSetup
{
    /// <summary>Call from Program.cs (both Api and Workers hosts) before building the WebApplication/Host:
    /// <c>builder.Host.UseSerilog((ctx, cfg) => SerilogSetup.Configure(cfg, ctx.Configuration, "Api"));</c></summary>
    public static void Configure(LoggerConfiguration loggerConfig, Microsoft.Extensions.Configuration.IConfiguration configuration, string serviceName)
    {
        loggerConfig
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] ({Service}) {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({Service}) {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}");
    }
}

/// <summary>ASP.NET Core middleware that reads/generates X-Correlation-Id and pushes it into the Serilog
/// LogContext for the lifetime of the request, so every log line (and the AuditLog) can be correlated.</summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value)
            ? value.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
