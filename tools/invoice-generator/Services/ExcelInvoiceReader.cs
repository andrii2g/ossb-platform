using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Osbb.InvoiceGenerator.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Osbb.InvoiceGenerator.Services;

public sealed class ExcelInvoiceReader
{
    private readonly CultureInfo _culture;

    public ExcelInvoiceReader(CultureInfo culture)
    {
        _culture = culture;
    }

    public IReadOnlyList<Invoice> ReadInvoices(InvoiceGeneratorConfig config)
    {
        using var workbook = new XLWorkbook(config.InputWorkbookPath);
        var sheet = workbook.Worksheet(1);

        var periodLabel = !string.IsNullOrWhiteSpace(config.ExplicitPeriodLabel)
            ? config.ExplicitPeriodLabel!.Trim()
            : DetectPeriodLabel(sheet);

        var rowHeader = FindRowContaining(sheet, "Особистий рахунок");
        var rowServiceNames = FindRowContaining(sheet, "Внесок на утримання будинку");

        var lastRow = sheet.LastRowUsed().RowNumber();

        var service1Name = sheet.Cell(rowServiceNames, 9).GetString().Trim();
        var service2Name = sheet.Cell(rowServiceNames, 10).GetString().Trim();

        var invoices = new List<Invoice>();

        for (var row = rowHeader + 4; row <= lastRow; row++)
        {
            var accountNumber = sheet.Cell(row, 2).GetString().Trim();

            if (string.IsNullOrWhiteSpace(accountNumber))
                continue;

            if (!Regex.IsMatch(accountNumber, @"^\d+$"))
                continue;

            var entrance = ParseInt(sheet.Cell(row, 3).GetString());
            var floor = ParseInt(sheet.Cell(row, 4).GetString());
            var apartment = ParseInt(sheet.Cell(row, 5).GetString());
            var ownerName = sheet.Cell(row, 6).GetString().Trim();

            var openingBalance = ParseDecimal(sheet.Cell(row, 7).GetString());
            var paidThisMonth = ParseDecimal(sheet.Cell(row, 8).GetString());

            var service1Amount = ParseDecimal(sheet.Cell(row, 9).GetString());
            var service2Amount = ParseDecimal(sheet.Cell(row, 10).GetString());
            var totalCharged = ParseDecimal(sheet.Cell(row, 11).GetString());
            var closingBalance = ParseDecimal(sheet.Cell(row, 12).GetString());
            var nextMonthPrepayment = ParseDecimal(sheet.Cell(row, 13).GetString());

            var isStorage = accountNumber.StartsWith("20", StringComparison.Ordinal);

            var quantity = config.ServiceRate == 0 ? 0 : totalCharged / config.ServiceRate;

            var items = new List<InvoiceLineItem>();
            items.Add(new InvoiceLineItem
            {
                Name = "Внесок на утримання будинку",
                Rate = config.ServiceRate,
                Quantity = quantity,
                SubsidyAmount = 0m,
                ChargedAmount = totalCharged
            });

            var qrUrl = "";
            var qrPngBase64 = "";
            if (closingBalance > 0)
            {
                var purpose = $"Внесок на утримання будинку; Харків, Драгоманова 6-Г/{accountNumber.Substring(1)}; {accountNumber}";

                var payload = BankQrPayloadBuilder.BuildOpenDataPayload(
                    recipientName: config.OrganizationName,
                    iban: config.Iban,
                    amount: closingBalance,
                    recipientCode: config.Edrpou,
                    paymentPurpose: purpose);

                qrUrl = BankQrPayloadBuilder.BuildQrUrl(payload);
                var qrPng = QrImageBuilder.BuildPng(qrUrl);
                qrPngBase64 = Convert.ToBase64String(qrPng);

                QrFileSaver.Save(config.OutputDirectory, accountNumber, qrPng);
            }

            invoices.Add(new Invoice
            {
                QrUrl = qrUrl,
                QrPngBytes = QrImageBuilder.BuildPng(qrUrl),
                AccountNumber = accountNumber,
                OwnerName = ownerName,
                AddressLine = BuildAddress(config, apartment, isStorage, accountNumber),
                PeriodLabel = periodLabel,
                Entrance = entrance,
                Floor = floor,
                ApartmentOrStorageNumber = apartment,
                IsStorage = isStorage,
                OpeningBalance = openingBalance,
                PaidThisMonth = paidThisMonth,
                TotalCharged = totalCharged,
                ClosingBalance = closingBalance,
                NextMonthPrepayment = nextMonthPrepayment,
                Items = items
            });
        }

        return invoices;
    }

    private string DetectPeriodLabel(IXLWorksheet sheet)
    {
        var line1 = sheet.Cell(4, 1).GetString().Trim();
        var line2 = sheet.Cell(5, 1).GetString().Trim();

        return string.IsNullOrWhiteSpace(line2)
            ? line1
            : $"{FirstCharToUpper(line2)}";
    }

    private static string FirstCharToUpper(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return char.ToUpper(value[0]) + value[1..];
    }

    private static string BuildAddress(InvoiceGeneratorConfig config, int apartment, bool isStorage, string accountNumber)
    {
        if (isStorage)
            return $"{config.City}, {config.Street}, буд. {config.Building}, комірка / нежитлове приміщення, о/р {accountNumber}";

        return $"{config.City}, {config.Street}, буд. {config.Building}, кв. {apartment}";
    }

    private int FindRowContaining(IXLWorksheet sheet, string text)
    {
        var lastRow = sheet.LastRowUsed().RowNumber();
        var lastColumn = sheet.LastColumnUsed().ColumnNumber();

        for (var row = 1; row <= lastRow; row++)
        {
            for (var col = 1; col <= lastColumn; col++)
            {
                if (sheet.Cell(row, col).GetString().Contains(text, StringComparison.OrdinalIgnoreCase))
                    return row;
            }
        }

        throw new InvalidOperationException($"Could not find text '{text}' in workbook.");
    }

    private decimal ParseDecimal(string raw)
    {
        raw = raw?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
            return 0m;

        raw = raw.Replace("\u00A0", string.Empty).Replace(" ", string.Empty);

        if (decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign, _culture, out var result))
            return result;

        if (decimal.TryParse(raw.Replace(",", "."), NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result))
            return result;

        throw new FormatException($"Could not parse decimal value '{raw}'.");
    }

    private static int ParseInt(string raw)
    {
        if (int.TryParse(raw?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;

        return 0;
    }
}
