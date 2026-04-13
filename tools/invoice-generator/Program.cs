using System.Globalization;
using Osbb.InvoiceGenerator.Models;
using Osbb.InvoiceGenerator.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var arguments = CliArguments.Parse(args);

if (arguments.ShowHelp)
{
    CliArguments.PrintHelp();
    return;
}

var culture = CultureInfo.GetCultureInfo("uk-UA");

var config = new InvoiceGeneratorConfig
{
    InputWorkbookPath = arguments.InputWorkbookPath,
    OutputDirectory = arguments.OutputDirectory,
    OrganizationName = arguments.OrganizationName,
    Edrpou = arguments.Edrpou,
    Iban = arguments.Iban,
    BankName = arguments.BankName,
    City = arguments.City,
    Street = arguments.Street,
    Building = arguments.Building,
    SupportPhone = arguments.SupportPhone,
    PaymentDeadlineText = arguments.PaymentDeadlineText,
    ExplicitPeriodLabel = arguments.ExplicitPeriodLabel,
    ServiceRate=arguments.Rate
};

Directory.CreateDirectory(config.OutputDirectory);

var reader = new ExcelInvoiceReader(culture);
var invoices = reader.ReadInvoices(config).ToList();

var renderer = new InvoicePdfRenderer(culture);

foreach (var invoice in invoices)
{
    var fileName = $"{invoice.AccountNumber}.pdf";
    var filePath = Path.Combine(config.OutputDirectory, fileName);
    renderer.Render(invoice, config, filePath);
    Console.WriteLine($"Generated: {filePath}");
}

Console.WriteLine($"Done. Created {invoices.Count} PDF file(s) in '{config.OutputDirectory}'.");

internal sealed class CliArguments
{
    public bool ShowHelp { get; init; }
    public required string InputWorkbookPath { get; init; }
    public required string OutputDirectory { get; init; }
    public required string OrganizationName { get; init; }
    public required string Edrpou { get; init; }
    public required string Iban { get; init; }
    public required string BankName { get; init; }
    public required string City { get; init; }
    public required string Street { get; init; }
    public required string Building { get; init; }
    public required string SupportPhone { get; init; }
    public required string PaymentDeadlineText { get; init; }
    public string? ExplicitPeriodLabel { get; init; }
    public required decimal Rate { get; init; }

    public static CliArguments Parse(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            return new CliArguments
            {
                ShowHelp = true,
                InputWorkbookPath = string.Empty,
                OutputDirectory = string.Empty,
                OrganizationName = string.Empty,
                Edrpou = string.Empty,
                Iban = string.Empty,
                BankName = string.Empty,
                City = string.Empty,
                Street = string.Empty,
                Building = string.Empty,
                SupportPhone = string.Empty,
                PaymentDeadlineText = string.Empty,
                Rate = 0
            };
        }

        string Require(string name, string? defaultValue=null)
        {
            var index = Array.IndexOf(args, name);
            if (index < 0 || index == args.Length - 1 || args[index + 1].StartsWith("--"))
                if (!string.IsNullOrEmpty(defaultValue))
                    return defaultValue!;
                else
                    throw new ArgumentException($"Missing required argument: {name}");
            return args[index + 1];
        }

        string? Optional(string name, string? defaultValue=null)
        {
            var index = Array.IndexOf(args, name);
            return index >= 0 && index < args.Length - 1 && !args[index + 1].StartsWith("--")
                ? args[index + 1]
                : defaultValue;
        }

        return new CliArguments
        {
            ShowHelp = false,
            InputWorkbookPath = Require("--input", @"C:\github\osbb-invoice-generator\in\6г-Д березень 2026.xlsx"),
            OutputDirectory = Require("--output", @"C:\github\osbb-invoice-generator\out"),
            OrganizationName = Require("--org-name", "ОСББ ДРАГОМАНОВА 6Г"),
            Edrpou = Require("--edrpou", "44485096"),
            Iban = Require("--iban", "UA283052990000026008005923121"),
            BankName = Require("--bank", "АТ КБ \"ПРИВАТБАНК\""),
            City = Optional("--city") ?? "м. Харків",
            Street = Require("--street", "вул. ДРАГОМАНОВА"),
            Building = Require("--building","6Г"),
            SupportPhone = Optional("--phone") ?? "067-57-47-138",
            PaymentDeadlineText = Optional("--deadline") ?? "Термін сплати до 20 числа поточного місяця",
            ExplicitPeriodLabel = Optional("--period","Березень 2026р."),
            Rate= decimal.TryParse(Require("--rate","6.24"), out var rate) ? rate : 0
        };
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""
OSBB Invoice Generator

Required:
  --input       Path to source XLSX workbook
  --output      Output folder for generated PDF files
  --org-name    Full organization name
  --edrpou      EDRPOU code
  --iban        IBAN / account
  --bank        Bank name
  --street      Street name (e.g. вул. ДРАГОМАНОВА)
  --building    Building number (e.g. 6Г)

Optional:
  --city        Default city, default: м. Харків
  --phone       Support phone
  --telegram    Telegram bot or contact
  --deadline    Payment deadline line
  --period      Override period label from workbook

Example:
  dotnet run -- ^
    --input "..\6г-Д березень 2026.xlsx" ^
    --output ".\out" ^
    --org-name "ОБ'ЄДНАННЯ СПІВВЛАСНИКІВ БАГАТОКВАРТИРНОГО БУДИНКУ ""ДРАГОМАНОВА 6Г ОСББ"" " ^
    --edrpou "44485096" ^
    --iban "UA283052990000026008005923121" ^
    --bank "АТ КБ ""ПРИВАТБАНК"" " ^
    --street "вул. ДРАГОМАНОВА" ^
    --building "6Г"
""");
    }
}
