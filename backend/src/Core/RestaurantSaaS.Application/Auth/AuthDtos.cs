namespace RestaurantSaaS.Application.Auth;

public sealed record AuthTokensDto(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);

public sealed record LoginResultDto(bool RequiresMfa, string? MfaChallengeToken, AuthTokensDto? Tokens);

public sealed record MfaEnrollmentResultDto(string Secret, string OtpAuthUri, IReadOnlyCollection<string> RecoveryCodes);
