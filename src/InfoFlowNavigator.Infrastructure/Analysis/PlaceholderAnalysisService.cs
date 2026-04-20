using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Infrastructure.Analysis;

public sealed class PlaceholderAnalysisService : IAnalysisService
{
    public Task<AnalysisSummary> SummarizeAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var summary = new AnalysisSummary(
            workspace.Entities.Count,
            workspace.Relationships.Count,
            workspace.Events.Count,
            workspace.Sources.Count);

        return Task.FromResult(summary);
    }
}
