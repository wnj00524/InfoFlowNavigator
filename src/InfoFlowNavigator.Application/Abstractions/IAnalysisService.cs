using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Application.Abstractions;

public interface IAnalysisService
{
    Task<AnalysisSummary> SummarizeAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default);
}
