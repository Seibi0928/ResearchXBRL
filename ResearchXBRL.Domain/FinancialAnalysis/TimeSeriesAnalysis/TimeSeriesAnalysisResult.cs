using System.Collections.Generic;
using ResearchXBRL.Domain.FinancialAnalysis.TimeSeriesAnalysis.Corporations;
using ResearchXBRL.Domain.FinancialReports.Units;

namespace ResearchXBRL.Domain.FinancialAnalysis.TimeSeriesAnalysis
{
    /// <summary>
    /// 時系列分析結果
    /// </summary>
    public sealed class TimeSeriesAnalysisResult
    {
        /// <summary>
        /// 会計項目名
        /// </summary>
        public string AccountName { get; init; } = "";

        /// <summary>
        /// 会計項目の単位
        /// </summary>
        public IUnit Unit { get; init; } = new NormalUnit();

        /// <summary>
        /// 企業情報
        /// </summary>
        public Corporation Corporation { get; init; } = new Corporation();

        /// <summary>
        /// 会計項目の時系列データ
        /// </summary>
        /// <typeparam name="AccountValue">会計項目値</typeparam>
        public IReadOnlyList<AccountValue> Values { get; init; } = new List<AccountValue>(0);
    }
}