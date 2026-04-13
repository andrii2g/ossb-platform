using Osbb.InvoiceGenerator.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Osbb.InvoiceGenerator.Services;

public sealed class InvoicePdfRenderer
{
    private readonly CultureInfo _culture;

    public InvoicePdfRenderer(CultureInfo culture)
    {
        _culture = culture;
    }

    private static string GetOpeningBalanceLabel(string? periodLabel)
    {
        if (string.IsNullOrWhiteSpace(periodLabel))
            return "Сальдо на початок місяця";

        var match = Regex.Match(periodLabel, @"(?<month>\p{L}+)\s+(?<year>\d{4})", RegexOptions.CultureInvariant);
        if (!match.Success)
            return "Сальдо на початок місяця";

        var monthName = match.Groups["month"].Value.Trim().ToLowerInvariant();
        var yearText = match.Groups["year"].Value;

        if (!int.TryParse(yearText, out var year))
            return "Сальдо на початок місяця";

        var month = monthName switch
        {
            "січень" => 1,
            "лютий" => 2,
            "березень" => 3,
            "квітень" => 4,
            "травень" => 5,
            "червень" => 6,
            "липень" => 7,
            "серпень" => 8,
            "вересень" => 9,
            "жовтень" => 10,
            "листопад" => 11,
            "грудень" => 12,
            _ => 0
        };

        if (month == 0)
            return "Сальдо на початок місяця";

        return $"Сальдо на {new DateTime(year, month, 1):dd.MM.yyyy}";
    }

    public void Render(Invoice invoice, InvoiceGeneratorConfig config, string outputPath)
    {
        var openingBalanceLabel = GetOpeningBalanceLabel(invoice.PeriodLabel);
        var closingBalanceLabel = invoice.ClosingBalance switch
        {
            > 0 => "Борг / до сплати",
            < 0 => "Переплата",
            _ => "Баланс"
        };

        var closingBalanceValue = invoice.ClosingBalance switch
        {
            > 0 => Money(invoice.ClosingBalance),
            < 0 => Money(Math.Abs(invoice.ClosingBalance)),
            _ => "0,00"
        };
        Document.Create(container =>
        {
            container.Page(page =>
            {
                var size = PageSizes.A5.Landscape();
                size=new PageSize(size.Width, size.Height-100);
                page.Size(size);
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(9));

                page.Content().Column(column =>
                {
                    column.Spacing(6);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem(2).Column(left =>
                        {
                            left.Item().Text("Квитанція на оплату внесків")
                                .Bold().FontSize(14);
                            left.Item().Text(invoice.PeriodLabel).SemiBold();
                            left.Item().Text($"Одержувач: {config.OrganizationName}");
                            left.Item().Text($"ЄДРПОУ: {config.Edrpou}");
                            left.Item().Text($"р/р № {config.Iban}; {config.BankName}");
                        });

                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().Text("Повідомлення").Bold();
                            right.Item().Text($"№ о/рахунка: {invoice.AccountNumber}");
                            right.Item().Text(invoice.IsStorage
                                ? "Тип: комірка / нежитлове приміщення"
                                : $"Квартира: {invoice.ApartmentOrStorageNumber}");
                        });
                    });

                    column.Item().Border(1).Padding(6).Column(block =>
                    {
                        block.Item().Text($"Платник: {invoice.OwnerName}").Bold();
                        block.Item().Text($"Адреса: {invoice.AddressLine}");
                    });

                    column.Item().Row(main =>
                    {
                        main.RelativeItem().Column(left =>
                        {
                            left.Spacing(6);

                            left.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3.6f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.4f);
                                    columns.RelativeColumn(1.8f);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    HeaderCell(header, "Вид платежу");
                                    HeaderCell(header, "Тариф (грн.)");
                                    HeaderCell(header, "Кількість (кв.м.)");
                                    HeaderCell(header, "Субс./Пільга (грн.)");
                                    HeaderCell(header, "Нараховано (грн.)");
                                });

                                foreach (var item in invoice.Items)
                                {
                                    BodyCell(table, item.Name);
                                    BodyCell(table, item.Rate == 0 ? "-" : Money(item.Rate));
                                    BodyCell(table, item.Quantity == 0 ? "-" : item.Quantity.ToString("N2", _culture));
                                    BodyCell(table, item.SubsidyAmount == 0 ? "0,00" : Money(item.SubsidyAmount));
                                    BodyCell(table, Money(item.ChargedAmount));
                                }
                            });

                            left.Item().Row(row =>
                            {
                                row.RelativeItem(3).Border(1).Padding(6).Column(balance =>
                                {
                                    balance.Item().Text($"Сальдо на {DateTime.Now.ToString("01.MM.yyyy")}: {Money(invoice.OpeningBalance)}").Bold();
                                    balance.Item().Text($"Нараховано: {Money(invoice.TotalCharged)}");
                                    balance.Item().Text($"Сплачено: {Money(invoice.PaidThisMonth)}");
                                    balance.Item().Text(
                                        $"{Money(invoice.OpeningBalance)} + {Money(invoice.TotalCharged)} - {Money(invoice.PaidThisMonth)} = {Money(invoice.ClosingBalance)}");
                                });

                                row.RelativeItem(3).Border(1).Padding(6).Column(result =>
                                {
                                    if (invoice.ClosingBalance > 0)
                                    {
                                        result.Item().Text($"Борг / до сплати: {Money(invoice.ClosingBalance)}")
                                            .Bold().FontSize(12);
                                    }
                                    else if (invoice.ClosingBalance < 0)
                                    {
                                        result.Item().Text($"Переплата: {Money(Math.Abs(invoice.ClosingBalance))}")
                                            .Bold().FontSize(12);
                                    }
                                    else
                                    {
                                        result.Item().Text("Баланс: 0,00").Bold().FontSize(12);
                                    }
                                });
                            });

                            left.Item().Border(0).Padding(12).Text(
                                $"{config.PaymentDeadlineText}. Тел. для довідок: {config.SupportPhone}");

                            //left.Item().Border(0).Padding(6).Text(
                            //    $"Підпис платника ____________________");
                        });

                        main.ConstantItem(165).PaddingLeft(6).Column(right =>
                        {
                            right.Item().Border(1).Padding(6).AlignTop().Column(qr =>
                            {


                                if (invoice.QrPngBytes is { Length: > 0 } && invoice.ClosingBalance > 0)
                                {
                                    qr.Item().Text("Відскануйте в банківському застосунку")
                                        .Bold().FontSize(8.5f).AlignCenter();
                                    qr.Item().AlignCenter().PaddingTop(8).Height(112).Image(invoice.QrPngBytes);
                                }
                                else
                                {
                                    qr.Item().Text("Переплата").Bold().FontSize(8.5f).AlignCenter();
                                    qr.Item().AlignCenter().PaddingTop(8).AlignMiddle().Height(112).Text("QR код не формується");
                                }
                            });
                        });
                    });

                });
            });
        }).GeneratePdf(outputPath);
    }

    private void HeaderCell(TableCellDescriptor header, string text)
    {
        header.Cell().Border(1).Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4)
            .AlignMiddle().Text(text).FontSize(8).SemiBold();
    }

    private void BodyCell(TableDescriptor table, string text)
    {
        table.Cell().Border(1).PaddingVertical(4).PaddingHorizontal(6).Text(text);
    }

    private string Money(decimal value) => value.ToString("N2", _culture);
}
