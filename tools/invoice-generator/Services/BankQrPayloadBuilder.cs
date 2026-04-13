
using System.Text;

namespace Osbb.InvoiceGenerator.Services;

public static class BankQrPayloadBuilder
{
    public static string BuildOpenDataPayload(
        string recipientName,
        string iban,
        decimal? amount,
        string recipientCode,
        string paymentPurpose)
    {
        var lines = new[]
        {
            "BCD",
            "002",
            "2",
            "UCT",
            string.Empty,
            recipientName,
            iban,
            amount.HasValue ? $"UAH{amount.Value:0.00}" : string.Empty,
            recipientCode,
            string.Empty,
            string.Empty,
            paymentPurpose,
            string.Empty
        };

        return string.Join("\n", lines);
    }

    public static string BuildQrUrl(string openDataPayload)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var win1251 = Encoding.GetEncoding(1251);
        var bytes = win1251.GetBytes(openDataPayload);
        var base64 = Convert.ToBase64String(bytes);
        var base64Url = base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return "https://bank.gov.ua/qr/" + base64Url;
    }
}
