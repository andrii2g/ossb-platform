namespace Osbb.InvoiceGenerator.Models;

public sealed class InvoiceGeneratorConfig
{
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
    public required decimal ServiceRate { get; init; }
    public string? ExplicitPeriodLabel { get; init; }
}

public sealed class Invoice
{
    public required string AccountNumber { get; init; }
    public required string OwnerName { get; init; }
    public required string AddressLine { get; init; }
    public required string PeriodLabel { get; init; }

    public int Entrance { get; init; }
    public int Floor { get; init; }
    public int ApartmentOrStorageNumber { get; init; }
    public bool IsStorage { get; init; }

    public decimal OpeningBalance { get; init; }
    public decimal PaidThisMonth { get; init; }
    public decimal TotalCharged { get; init; }
    public decimal ClosingBalance { get; init; }
    public decimal NextMonthPrepayment { get; init; }

    public required IReadOnlyList<InvoiceLineItem> Items { get; init; }

    public decimal AmountDue => ClosingBalance > 0 ? ClosingBalance : 0m;
    public decimal CreditBalance => ClosingBalance < 0 ? Math.Abs(ClosingBalance) : 0m;

    public string QrUrl { get; init; } = "";
    public byte[]? QrPngBytes { get; init; } = null;
}

public sealed class InvoiceLineItem
{
    public required string Name { get; init; }
    public decimal Rate { get; init; }
    public decimal Quantity { get; init; }
    public decimal SubsidyAmount { get; init; }
    public decimal ChargedAmount { get; init; }
}
