using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Application.Abstractions;

public interface IWorkspaceImportService
{
    Task<AnalysisWorkspace> ImportAsync(string path, CancellationToken cancellationToken = default);
}
