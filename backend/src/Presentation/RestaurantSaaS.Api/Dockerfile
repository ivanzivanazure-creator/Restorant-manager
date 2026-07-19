# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY src/Core/RestaurantSaaS.Domain/RestaurantSaaS.Domain.csproj src/Core/RestaurantSaaS.Domain/
COPY src/Core/RestaurantSaaS.Application/RestaurantSaaS.Application.csproj src/Core/RestaurantSaaS.Application/
COPY src/Infrastructure/RestaurantSaaS.Infrastructure/RestaurantSaaS.Infrastructure.csproj src/Infrastructure/RestaurantSaaS.Infrastructure/
COPY src/Presentation/RestaurantSaaS.Api/RestaurantSaaS.Api.csproj src/Presentation/RestaurantSaaS.Api/
RUN dotnet restore src/Presentation/RestaurantSaaS.Api/RestaurantSaaS.Api.csproj

COPY src/Core/RestaurantSaaS.Domain/ src/Core/RestaurantSaaS.Domain/
COPY src/Core/RestaurantSaaS.Application/ src/Core/RestaurantSaaS.Application/
COPY src/Infrastructure/RestaurantSaaS.Infrastructure/ src/Infrastructure/RestaurantSaaS.Infrastructure/
COPY src/Presentation/RestaurantSaaS.Api/ src/Presentation/RestaurantSaaS.Api/

RUN dotnet publish src/Presentation/RestaurantSaaS.Api/RestaurantSaaS.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN useradd --uid 5678 --user-group --system --no-create-home appuser
COPY --from=build /app .
RUN mkdir -p /app/wwwroot/uploads /app/logs && chown -R appuser:appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RestaurantSaaS.Api.dll"]
