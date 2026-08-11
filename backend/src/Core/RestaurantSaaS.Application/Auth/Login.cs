using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Identity;

namespace RestaurantSaaS.Application.Auth;

public sealed record LoginCommand(string Email, string Password, string DeviceInfo) : IRequest<LoginResultDto>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IIdentityService identityService, ICacheService cache, ICurrentUserService currentUser, TokenIssuer tokenIssuer)
    : IRequestHandler<LoginCommand, LoginResultDto>
{
    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await identityService.FindByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive) throw new UnauthorizedAccessException("Invalid email or password.");

        if (await identityService.IsLockedOutAsync(user.Id, ct))
            throw new UnauthorizedAccessException("Account is temporarily locked due to too many failed attempts.");

        var passwordOk = await identityService.CheckPasswordAsync(user.Id, request.Password, ct);
        await identityService.RecordLoginAttemptAsync(user.Id, passwordOk, ct);
        if (!passwordOk) throw new UnauthorizedAccessException("Invalid email or password.");

        if (user.MfaEnabled)
        {
            var challengeToken = Guid.NewGuid().ToString("N");
            await cache.SetAsync($"mfa-challenge:{challengeToken}", user.Id, TimeSpan.FromMinutes(5), ct);
            return new LoginResultDto(RequiresMfa: true, challengeToken, Tokens: null);
        }

        var tokens = await tokenIssuer.IssueAsync(user, request.DeviceInfo, currentUser.IpAddress ?? "unknown", ct);
        return new LoginResultDto(RequiresMfa: false, MfaChallengeToken: null, tokens);
    }
}

public sealed record VerifyMfaCommand(string MfaChallengeToken, string Code, string DeviceInfo) : IRequest<AuthTokensDto>;

public sealed class VerifyMfaCommandValidator : AbstractValidator<VerifyMfaCommand>
{
    public VerifyMfaCommandValidator()
    {
        RuleFor(x => x.MfaChallengeToken).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}

public sealed class VerifyMfaCommandHandler(
    IApplicationDbContext db, IIdentityService identityService, IMfaService mfaService, ICacheService cache,
    ICurrentUserService currentUser, TokenIssuer tokenIssuer)
    : IRequestHandler<VerifyMfaCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(VerifyMfaCommand request, CancellationToken ct)
    {
        var cacheKey = $"mfa-challenge:{request.MfaChallengeToken}";
        var userId = await cache.GetAsync<Guid?>(cacheKey, ct)
            ?? throw new UnauthorizedAccessException("MFA challenge expired or invalid; please log in again.");

        var enrollment = await db.Set<MfaEnrollment>().SingleOrDefaultAsync(m => m.UserId == userId && m.IsEnabled, ct)
            ?? throw new UnauthorizedAccessException("MFA is not enrolled for this account.");

        if (!mfaService.ValidateCode(enrollment.EncryptedSecret, request.Code))
            throw new UnauthorizedAccessException("Invalid authentication code.");

        await cache.RemoveAsync(cacheKey, ct);

        var user = await identityService.FindByIdAsync(userId, ct) ?? throw new UnauthorizedAccessException("User not found.");
        return await tokenIssuer.IssueAsync(user, request.DeviceInfo, currentUser.IpAddress ?? "unknown", ct);
    }
}
