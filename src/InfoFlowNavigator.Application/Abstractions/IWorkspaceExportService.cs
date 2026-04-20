using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Application.Abstractions;

public interface IWorkspaceExportService
{
    Task ExportAsync(AnalysisWorkspace workspace, string path, CancellationToken cancellationToken = default);
}
