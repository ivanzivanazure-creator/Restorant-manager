using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using OtpNet;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Infrastructure.Identity;

/// <summary>Thin wrapper around ASP.NET Core Identity's PasswordHasher so Application code never
/// references Identity types directly.</summary>
public sealed class IdentityPasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<ApplicationUser> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string hash) =>
        _hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}

public sealed class TotpMfaService : IMfaService
{
    public (string Secret, string QrCodeImageDataUri) GenerateEnrollment(string email)
    {
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(secretBytes);
        var otpAuthUri = $"otpauth://totp/RestaurantSaaS:{Uri.EscapeDataString(email)}?secret={secret}&issuer=RestaurantSaaS&digits=6&period=30";

        // The QR image itself is rendered client-side (or via IQrCodeGenerator) from this URI; we return
        // the URI as the "image" payload here to keep this service focused on the TOTP secret lifecycle.
        return (secret, otpAuthUri);
    }

    public bool ValidateCode(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
    }

    public IReadOnlyCollection<string> GenerateRecoveryCodes(int count = 8)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var bytes = RandomNumberGenerator.GetBytes(6);
            codes.Add(Convert.ToHexString(bytes));
        }
        return codes;
    }
}
