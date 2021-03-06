using System;

namespace ResearchXBRL.Domain.ReverseLookupAccountItems.AccountItems;

public record AccountItem(string NormalizedName, string OriginalName, int SecuritiesCode, DateOnly FiscalYear);