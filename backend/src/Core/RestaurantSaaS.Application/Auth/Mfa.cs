using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Identity;

namespace RestaurantSaaS.Application.Auth;

public sealed record EnrollMfaCommand : IRequest<MfaEnrollmentResultDto>;

public sealed class EnrollMfaCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMfaService mfaService)
    : IRequestHandler<EnrollMfaCommand, MfaEnrollmentResultDto>
{
    public async Task<MfaEnrollmentResultDto> Handle(EnrollMfaCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var email = currentUser.Email ?? throw new UnauthorizedAccessException();

        var existing = await db.Set<MfaEnrollment>().SingleOrDefaultAsync(m => m.UserId == userId, ct);
        if (existing is { IsEnabled: true }) throw new ConflictException("MFA is already enabled for this account.");

        var (secret, otpAuthUri) = mfaService.GenerateEnrollment(email);
        var recoveryCodes = mfaService.GenerateRecoveryCodes();
        var recoveryHashes = recoveryCodes.Select(HashRecoveryCode).ToList();

        if (existing is not null)
        {
            db.Set<MfaEnrollment>().Remove(existing); // replace any prior, unconfirmed enrollment attempt
        }

        // NOTE: production deployments should encrypt `secret` at rest (e.g. via Azure Key Vault / the
        // ASP.NET Core Data Protection API) before it reaches MfaEnrollment.EncryptedSecret — Phase 1
        // stores it as returned by IMfaService; see docs/ROADMAP.md.
        db.Set<MfaEnrollment>().Add(new MfaEnrollment(userId, secret, recoveryHashes));
        await db.SaveChangesAsync(ct);

        return new MfaEnrollmentResultDto(secret, otpAuthUri, recoveryCodes);
    }

    internal static string HashRecoveryCode(string code) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)));
}

public sealed record ConfirmMfaEnrollmentCommand(string Code) : IRequest;

public sealed class ConfirmMfaEnrollmentCommandValidator : AbstractValidator<ConfirmMfaEnrollmentCommand>
{
    public ConfirmMfaEnrollmentCommandValidator() => RuleFor(x => x.Code).NotEmpty().Length(6);
}

public sealed class ConfirmMfaEnrollmentCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IMfaService mfaService, IIdentityService identityService)
    : IRequestHandler<ConfirmMfaEnrollmentCommand>
{
    public async Task Handle(ConfirmMfaEnrollmentCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var enrollment = await db.Set<MfaEnrollment>().SingleOrDefaultAsync(m => m.UserId == userId && !m.IsEnabled, ct)
            ?? throw new NotFoundException(nameof(MfaEnrollment), userId);

        if (!mfaService.ValidateCode(enrollment.EncryptedSecret, request.Code))
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.Code), "Invalid authentication code.")]);

        enrollment.Activate();
        await identityService.SetMfaEnabledAsync(userId, true, ct);
        await db.SaveChangesAsync(ct);
    }
}
