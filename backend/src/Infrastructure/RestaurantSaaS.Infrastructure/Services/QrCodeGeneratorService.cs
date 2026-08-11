using QRCoder;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Infrastructure.Services;

public sealed class QrCodeGeneratorService : IQrCodeGenerator
{
    public string GenerateDataUri(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var pngBytes = new PngByteQRCode(data).GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }
}
