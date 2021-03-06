using System;
using System.Collections.Generic;

namespace ResearchXBRL.Application.ViewModel.FinancialAnalysis.PerformanceIndicators.Indicators;

public class IndicatorViewModel
{
    public IndicatorTypeViewModel IndicatorType { get; init; }

    public IReadOnlyDictionary<DateTime, decimal> Values { get; init; } = new Dictionary<DateTime, decimal>();
}

public enum IndicatorTypeViewModel
{

    /// <summary>
    /// 売上高
    /// </summary>
    NetSales = 0,

    /// <summary>
    /// 営業利益
    /// </summary>
    OperatingIncome = 1,

    /// <summary>
    /// 経常利益
    /// </summary>
    OrdinaryIncome = 2,

    /// <summary>
    /// 親会社の所有者に帰属する利益
    /// </summary>
    ProfitLossAttributableToOwnersOfParent = 3,

    /// <summary>
    /// ROE
    /// </summary>
    RateOfReturnOnEquitySummaryOfBusinessResults = 4,

    /// <summary>
    /// １株当たり配当
    /// </summary>
    DividendPaidPerShareSummaryOfBusinessResults = 5
}
