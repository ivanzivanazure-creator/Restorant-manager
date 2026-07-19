using Hangfire;
using RestaurantSaaS.Application;
using RestaurantSaaS.Infrastructure;
using RestaurantSaaS.Infrastructure.Logging;
using RestaurantSaaS.Workers.Jobs;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((sp, cfg) => SerilogSetup.Configure(cfg, builder.Configuration, "Workers"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHangfireServer(options => options.Queues = ["default", "reports", "alerts"]);

builder.Services.AddScoped<SubscriptionExpirationJob>();
builder.Services.AddScoped<LowStockAlertJob>();
builder.Services.AddScoped<DailyReportAggregationJob>();

var app = builder.Build();

RecurringJob.AddOrUpdate<SubscriptionExpirationJob>(
    "subscription-expiration", job => job.RunAsync(CancellationToken.None), Cron.Hourly, new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

RecurringJob.AddOrUpdate<LowStockAlertJob>(
    "low-stock-alerts", job => job.RunAsync(CancellationToken.None), "*/15 * * * *", new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

RecurringJob.AddOrUpdate<DailyReportAggregationJob>(
    "daily-report-aggregation", job => job.RunAsync(CancellationToken.None), Cron.Daily(1, 0), new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

app.Run();
