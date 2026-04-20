using InfoFlowNavigator.Application.Reporting;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Application.Abstractions;

public interface IReportGenerator
{
    Task<ReportArtifact> GenerateAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default);
}
