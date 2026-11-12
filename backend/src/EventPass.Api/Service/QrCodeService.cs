using QRCoder;

namespace EventPass.Api.Services;

public interface IQrCodeService
{
    string GenerateBase64Png(string content);
}

public sealed class QrCodeService : IQrCodeService
{
    public string GenerateBase64Png(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return Convert.ToBase64String(png.GetGraphic(10));
    }
}
