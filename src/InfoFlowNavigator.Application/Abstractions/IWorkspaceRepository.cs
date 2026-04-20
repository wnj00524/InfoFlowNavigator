using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Application.Abstractions;

public interface IWorkspaceRepository
{
    Task<AnalysisWorkspace> LoadAsync(string path, CancellationToken cancellationToken = default);

    Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default);
}
