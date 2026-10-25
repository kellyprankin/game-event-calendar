using QRCoder;

namespace GameEventApi.Services;

/// <summary>
/// Builds the public registration link that the QR code encodes. PUBLIC_BASE_URL wins when
/// set (e.g. your PC's LAN address so a phone can reach it); otherwise the link uses the
/// host the page was loaded from.
/// </summary>
public class RegistrationLinks(IConfiguration config)
{
    public string RegistrationUrl(HttpRequest request, Guid eventId)
    {
        var baseUrl = config["PUBLIC_BASE_URL"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = $"{request.Scheme}://{request.Host}";
        }
        return $"{baseUrl.TrimEnd('/')}/events/{eventId}/register";
    }

    public static byte[] QrPng(string url)
    {
        using var data = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(pixelsPerModule: 10);
    }
}
