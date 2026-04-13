using QRCoder;

namespace Osbb.InvoiceGenerator.Services;

public static class QrImageBuilder
{
    public static byte[] BuildPng(string qrUrl, int pixelsPerModule = 8)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.M);
        var pngQr = new PngByteQRCode(qrData);
        return pngQr.GetGraphic(pixelsPerModule);
    }
}
