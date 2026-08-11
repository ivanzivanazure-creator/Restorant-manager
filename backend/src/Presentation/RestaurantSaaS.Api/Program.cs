using System.Text.Json.Serialization;
using Asp.Versioning;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using RestaurantSaaS.Api.Middleware;
using RestaurantSaaS.Application;
using RestaurantSaaS.Infrastructure;
using RestaurantSaaS.Infrastructure.HealthChecks;
using RestaurantSaaS.Infrastructure.Identity;
using RestaurantSaaS.Infrastructure.Logging;
using RestaurantSaaS.Infrastructure.Persistence;
using RestaurantSaaS.Infrastructure.Persistence.Seed;
using RestaurantSaaS.Infrastructure.RealTime;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, cfg) => SerilogSetup.Configure(cfg, context.Configuration, "Api"));

// ---- Services ----

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Restaurant SaaS API",
        Version = "v1",
        Description = "Multi-tenant restaurant management platform — POS, Kitchen Display, Inventory, Menu, and more.",
    });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste a JWT access token (without the 'Bearer ' prefix).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [jwtScheme] = [] });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials(); // credentials needed for SignalR
    });
});

builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres")
    .AddCheck<RedisHealthCheck>("redis");

var app = builder.Build();

// ---- Startup: migrate + seed ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var seedDemoData = builder.Configuration.GetValue("SeedDemoData", app.Environment.IsDevelopment());

    await db.Database.MigrateAsync();

    await DbSeeder.SeedAsync(db, userManager, logger, seedDemoData);
}

// ---- Middleware pipeline ----

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Modern OpenAPI explorer (scalar.com) reading the same Swashbuckle-generated document as above —
    // reachable at /scalar/v1.
    app.MapScalarApiReference(options => options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json"));
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // serves /uploads (LocalFileStorageService) — swap for Azure Blob + CDN in production
app.UseCors("AllowConfiguredOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<KitchenHub>("/hubs/kitchen");
app.MapHub<OrdersHub>("/hubs/orders");

// liveness: process is up, no dependency checks run
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

// readiness: DB + Redis reachable; body breaks down per-check status for diagnosability
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                error = e.Value.Exception?.Message,
            }),
        });
    },
});

app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireSuperAdminAuthFilter()],
});

app.Run();

/// <summary>Restricts the Hangfire dashboard to authenticated SuperAdmin principals.</summary>
file sealed class HangfireSuperAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.HasClaim(ClaimTypesExt.SuperAdmin, "true");
    }
}

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
