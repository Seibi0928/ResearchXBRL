using System.Threading.Tasks;
using ResearchXBRL.Application.ViewModel.FinancialAnalysis.TimeSeriesAnalysis;

namespace ResearchXBRL.Application.Usecase.FinancialAnalysis.TimeSeriesAnalysis;

public interface IPerformTimeSeriesAnalysisUsecase
{
    Task<TimeSeriesAnalysisViewModel> Handle(AnalyticalMaterials input);
}
