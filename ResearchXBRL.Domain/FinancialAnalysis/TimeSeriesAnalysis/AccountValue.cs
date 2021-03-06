using ResearchXBRL.Domain.FinancialAnalysis.TimeSeriesAnalysis.AccountPeriods;

namespace ResearchXBRL.Domain.FinancialAnalysis.TimeSeriesAnalysis
{
    public class AccountValue
    {
        /// <summary>
        /// 決算期
        /// </summary>
        public IAccountsPeriod FinancialAccountPeriod { get; init; } = new InstantPeriod();

        /// <summary>
        /// 金額
        /// </summary>
        public decimal Amount { get; init; }
    }
}
