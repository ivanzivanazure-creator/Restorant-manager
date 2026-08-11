using FluentValidation;
using MediatR;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Application.Auth;

/// <summary>Always succeeds from the caller's perspective (no user-enumeration signal) even if the email
/// doesn't exist; the reset email is only actually sent when it does.</summary>
public sealed record ForgotPasswordCommand(string Email, string ResetUrlTemplate) : IRequest;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

public sealed class ForgotPasswordCommandHandler(IIdentityService identityService, INotificationSender notificationSender)
    : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await identityService.FindByEmailAsync(request.Email, ct);
        if (user is null) return;

        var token = await identityService.GeneratePasswordResetTokenAsync(user.Id, ct);
        var resetUrl = request.ResetUrlTemplate
            .Replace("{userId}", user.Id.ToString())
            .Replace("{token}", Uri.EscapeDataString(token));

        await notificationSender.SendAsync(NotificationChannelKind.Email, user.Email, "Reset your password",
            $"<p>Hi {user.FirstName},</p><p>Click <a href=\"{resetUrl}\">here</a> to reset your password. This link expires shortly.</p>", ct);
    }
}

public sealed record ResetPasswordCommand(Guid UserId, string Token, string NewPassword) : IRequest;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(10);
    }
}

public sealed class ResetPasswordCommandHandler(IIdentityService identityService) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var (succeeded, errors) = await identityService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword, ct);
        if (!succeeded)
            throw new Common.Exceptions.ValidationException(errors.Select(e => new FluentValidation.Results.ValidationFailure(nameof(request.NewPassword), e)));
    }
}
