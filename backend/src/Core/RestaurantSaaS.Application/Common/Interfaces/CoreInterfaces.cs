using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Application.Common.Interfaces;

/// <summary>Resolves the current tenant (RestaurantOwner id) from the authenticated principal's JWT claims.
/// Backs the EF Core global query filter; SuperAdmin principals resolve to IsSuperAdmin = true and no TenantId.</summary>
public interface ITenantProvider
{
    Guid? TenantId { get; }
    Guid? LocationId { get; }
    bool IsSuperAdmin { get; }
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Permissions { get; }
    string? IpAddress { get; }
}

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);

public interface IJwtTokenService
{
    TokenPair GenerateTokenPair(Guid userId, string email, Guid? tenantId, Guid? locationId,
        IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, bool isSuperAdmin, string deviceInfo, string ipAddress);

    /// <summary>Rotates a refresh token: validates it, revokes it, and issues a fresh pair.</summary>
    Task<TokenPair> RefreshAsync(string refreshToken, string deviceInfo, string ipAddress, CancellationToken ct);

    Task RevokeAsync(string refreshToken, CancellationToken ct);
}

public interface IPasswordHasherService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IMfaService
{
    (string Secret, string QrCodeImageDataUri) GenerateEnrollment(string email);
    bool ValidateCode(string secret, string code);
    IReadOnlyCollection<string> GenerateRecoveryCodes(int count = 8);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

public interface IQrCodeGenerator
{
    /// <returns>A data: URI (PNG) embedding the QR code for the given payload; also used to generate a
    /// permanently hosted image via IFileStorageService in a real deployment.</returns>
    string GenerateDataUri(string payload);
}

public interface IFileStorageService
{
    Task<string> UploadAsync(string containerName, string fileName, Stream content, string contentType, CancellationToken ct);
}

public enum NotificationChannelKind { Email, Sms, Push }

public interface INotificationSender
{
    Task SendAsync(NotificationChannelKind channel, string recipient, string subject, string body, CancellationToken ct);
}

public interface IPaymentGatewayService
{
    Task<string> CreateCustomerAsync(string email, string name, CancellationToken ct);
    Task<string> CreateSubscriptionAsync(string customerId, string priceId, CancellationToken ct);
    Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct);

    /// <summary>Creates a Stripe Connect (Express) account for a tenant so their card payments can be
    /// split-paid to them minus the platform's transaction fee. Returns the connected account id.</summary>
    Task<string> CreateConnectedAccountAsync(string tenantContactEmail, string companyName, CancellationToken ct);

    /// <summary>A hosted onboarding link (KYC, bank details) the tenant is redirected to after
    /// CreateConnectedAccountAsync; expires after a few minutes per Stripe's own policy.</summary>
    Task<string> CreateAccountOnboardingLinkAsync(string connectedAccountId, string returnUrl, string refreshUrl, CancellationToken ct);

    /// <summary>Captures a card payment with an application fee routed to the platform's own Stripe
    /// account, the rest to the tenant's connected account. Returns the Stripe PaymentIntent id.</summary>
    Task<string> CapturePaymentWithApplicationFeeAsync(
        string connectedAccountId, decimal amount, string currency, decimal applicationFeeAmount, CancellationToken ct);
}

/// <summary>Publishes real-time events to SignalR hub groups. Implemented in Infrastructure to keep
/// Application decoupled from the SignalR transport.</summary>
public interface IRealtimeNotifier
{
    Task NotifyKitchenAsync(Guid locationId, object payload, CancellationToken ct = default);
    Task NotifyOrdersAsync(Guid locationId, object payload, CancellationToken ct = default);
}

/// <summary>Live health probe backing the public status page. Api/Realtime/BackgroundJobs are reported
/// Operational by definition (if this code is executing, the API process is up) unless an open incident
/// says otherwise; Database/Cache are checked live via a real connection attempt — see
/// PlatformHealthCheckerService in Infrastructure.</summary>
public interface IPlatformHealthChecker
{
    Task<IReadOnlyDictionary<PlatformComponent, ComponentHealth>> CheckAsync(CancellationToken ct);
}
