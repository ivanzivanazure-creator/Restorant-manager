using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSaaS.Application.Auth;

namespace RestaurantSaaS.Api.Controllers;

[AllowAnonymous]
public sealed class AuthController(ISender mediator) : ApiControllerBase(mediator)
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthTokensDto>> Register(RegisterOwnerCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command, ct));

    [HttpPost("login")]
    public async Task<ActionResult<LoginResultDto>> Login(LoginCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command, ct));

    [HttpPost("mfa/verify")]
    public async Task<ActionResult<AuthTokensDto>> VerifyMfa(VerifyMfaCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command, ct));

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthTokensDto>> Refresh(RefreshTokenCommand command, CancellationToken ct) =>
        Ok(await Mediator.Send(command, ct));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return NoContent(); // always 204, regardless of whether the email exists — avoids user enumeration
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("mfa/enroll")]
    [Authorize]
    public async Task<ActionResult<MfaEnrollmentResultDto>> EnrollMfa(CancellationToken ct) =>
        Ok(await Mediator.Send(new EnrollMfaCommand(), ct));

    [HttpPost("mfa/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmMfaEnrollment(ConfirmMfaEnrollmentCommand command, CancellationToken ct)
    {
        await Mediator.Send(command, ct);
        return NoContent();
    }
}
