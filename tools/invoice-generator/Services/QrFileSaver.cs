namespace Osbb.InvoiceGenerator.Services;

public static class QrFileSaver
{
    public static void Save(string outputDir, string accountNumber, byte[] qrPng)
    {
        var qrDir = outputDir;

        if (!Directory.Exists(qrDir))
            Directory.CreateDirectory(qrDir);

        var filePath = Path.Combine(qrDir, $"{accountNumber}.png");

        File.WriteAllBytes(filePath, qrPng);
    }
}