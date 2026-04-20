using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Application.Abstractions;

public interface IAnalysisService
{
    Task<WorkspaceAnalysisResult> SummarizeAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default);
}
