using System.Globalization;
using Microsoft.Extensions.Configuration;
using Osbb.InvoiceGenerator.Models;
using Osbb.InvoiceGenerator.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var appConfiguration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>(optional: true, reloadOnChange: false)
    .Build();

var defaults = appConfiguration.GetSection("Defaults").Get<CliArgumentDefaults>() ?? new CliArgumentDefaults();
var arguments = CliArguments.Parse(args, defaults);

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
    ServiceRate = arguments.Rate
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

    public static CliArguments Parse(string[] args, CliArgumentDefaults defaults)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            return Empty(showHelp: true);
        }

        string Require(string name, string? fallback = null)
        {
            var value = Optional(name, fallback);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Missing required argument: {name}. Provide it via CLI or appsettings.json.");
            }

            return value;
        }

        string? Optional(string name, string? fallback = null)
        {
            var index = Array.IndexOf(args, name);
            if (index >= 0)
            {
                if (index == args.Length - 1 || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Missing value for argument: {name}");
                }

                return args[index + 1];
            }

            return fallback;
        }

        if (args.Length == 0 && !defaults.HasAnyValue)
        {
            return Empty(showHelp: true);
        }

        var rateText = Require("--rate", defaults.Rate);
        if (!decimal.TryParse(rateText, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) &&
            !decimal.TryParse(rateText, NumberStyles.Number, CultureInfo.GetCultureInfo("uk-UA"), out rate))
        {
            throw new ArgumentException("Invalid value for --rate. Use a decimal number in CLI or appsettings.json.");
        }

        return new CliArguments
        {
            ShowHelp = false,
            InputWorkbookPath = Require("--input", defaults.InputWorkbookPath),
            OutputDirectory = Require("--output", defaults.OutputDirectory),
            OrganizationName = Require("--org-name", defaults.OrganizationName),
            Edrpou = Require("--edrpou", defaults.Edrpou),
            Iban = Require("--iban", defaults.Iban),
            BankName = Require("--bank", defaults.BankName),
            City = Require("--city", defaults.City),
            Street = Require("--street", defaults.Street),
            Building = Require("--building", defaults.Building),
            SupportPhone = Require("--phone", defaults.SupportPhone),
            PaymentDeadlineText = Require("--deadline", defaults.PaymentDeadlineText),
            ExplicitPeriodLabel = Optional("--period", defaults.ExplicitPeriodLabel),
            Rate = rate
        };
    }

    private static CliArguments Empty(bool showHelp) => new()
    {
        ShowHelp = showHelp,
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
        Rate = 0m
    };

    public static void PrintHelp()
    {
        Console.WriteLine("""
OSBB Invoice Generator

Arguments may be provided directly on the command line or via the Defaults section in appsettings.json.
Command-line values override appsettings.json.

Required:
  --input       Path to source XLSX workbook
  --output      Output folder for generated PDF files
  --org-name    Full organization name
  --edrpou      EDRPOU code
  --iban        IBAN / account
  --bank        Bank name
  --street      Street name
  --building    Building number
  --rate        Service rate per square meter

Optional:
  --city        Default city
  --phone       Support phone
  --deadline    Payment deadline line
  --period      Override period label from workbook

Example:
  dotnet run -- \
    --input "./in/invoices.xlsx" \
    --output "./out" \
    --org-name "OSBB Example" \
    --edrpou "12345678" \
    --iban "UA281234560000026008001234567" \
    --bank "Bank Name" \
    --street "Example Street" \
    --building "12A" \
    --rate "6.24"
""");
    }
}

internal sealed class CliArgumentDefaults
{
    public string? InputWorkbookPath { get; init; }
    public string? OutputDirectory { get; init; }
    public string? OrganizationName { get; init; }
    public string? Edrpou { get; init; }
    public string? Iban { get; init; }
    public string? BankName { get; init; }
    public string? City { get; init; }
    public string? Street { get; init; }
    public string? Building { get; init; }
    public string? SupportPhone { get; init; }
    public string? PaymentDeadlineText { get; init; }
    public string? ExplicitPeriodLabel { get; init; }
    public string? Rate { get; init; }

    public bool HasAnyValue =>
        !string.IsNullOrWhiteSpace(InputWorkbookPath) ||
        !string.IsNullOrWhiteSpace(OutputDirectory) ||
        !string.IsNullOrWhiteSpace(OrganizationName) ||
        !string.IsNullOrWhiteSpace(Edrpou) ||
        !string.IsNullOrWhiteSpace(Iban) ||
        !string.IsNullOrWhiteSpace(BankName) ||
        !string.IsNullOrWhiteSpace(City) ||
        !string.IsNullOrWhiteSpace(Street) ||
        !string.IsNullOrWhiteSpace(Building) ||
        !string.IsNullOrWhiteSpace(SupportPhone) ||
        !string.IsNullOrWhiteSpace(PaymentDeadlineText) ||
        !string.IsNullOrWhiteSpace(ExplicitPeriodLabel) ||
        !string.IsNullOrWhiteSpace(Rate);
}
