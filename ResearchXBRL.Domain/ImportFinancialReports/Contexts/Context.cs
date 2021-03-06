namespace ResearchXBRL.Domain.ImportFinancialReports.Contexts;

public sealed class Context
{
    public string Name { get; init; } = "";
    public IPeriod Period { get; init; } = new InstantPeriod();
}
