using FluentValidation;
using MediatR;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Application.Auth;

public sealed record RefreshTokenCommand(string RefreshToken, string DeviceInfo) : IRequest<AuthTokensDto>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RefreshTokenCommandHandler(IJwtTokenService jwtTokenService, ICurrentUserService currentUser)
    : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var pair = await jwtTokenService.RefreshAsync(request.RefreshToken, request.DeviceInfo, currentUser.IpAddress ?? "unknown", ct);
        return new AuthTokensDto(pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAt);
    }
}

public sealed record LogoutCommand(string RefreshToken) : IRequest;

public sealed class LogoutCommandHandler(IJwtTokenService jwtTokenService) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct) => await jwtTokenService.RevokeAsync(request.RefreshToken, ct);
}
