# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY src/Core/RestaurantSaaS.Domain/RestaurantSaaS.Domain.csproj src/Core/RestaurantSaaS.Domain/
COPY src/Core/RestaurantSaaS.Application/RestaurantSaaS.Application.csproj src/Core/RestaurantSaaS.Application/
COPY src/Infrastructure/RestaurantSaaS.Infrastructure/RestaurantSaaS.Infrastructure.csproj src/Infrastructure/RestaurantSaaS.Infrastructure/
COPY src/Workers/RestaurantSaaS.Workers/RestaurantSaaS.Workers.csproj src/Workers/RestaurantSaaS.Workers/
RUN dotnet restore src/Workers/RestaurantSaaS.Workers/RestaurantSaaS.Workers.csproj

COPY src/Core/RestaurantSaaS.Domain/ src/Core/RestaurantSaaS.Domain/
COPY src/Core/RestaurantSaaS.Application/ src/Core/RestaurantSaaS.Application/
COPY src/Infrastructure/RestaurantSaaS.Infrastructure/ src/Infrastructure/RestaurantSaaS.Infrastructure/
COPY src/Workers/RestaurantSaaS.Workers/ src/Workers/RestaurantSaaS.Workers/

RUN dotnet publish src/Workers/RestaurantSaaS.Workers/RestaurantSaaS.Workers.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN useradd --uid 5678 --user-group --system --no-create-home appuser
COPY --from=build /app .
RUN mkdir -p /app/logs && chown -R appuser:appuser /app
USER appuser

# No HTTP listener — this host only runs the Hangfire job processor (the dashboard is served by the API).
ENTRYPOINT ["dotnet", "RestaurantSaaS.Workers.dll"]
