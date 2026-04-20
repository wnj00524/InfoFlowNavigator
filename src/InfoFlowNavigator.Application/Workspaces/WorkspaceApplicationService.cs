using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Application.Workspaces;

public sealed class WorkspaceApplicationService
{
    private readonly IWorkspaceRepository _workspaceRepository;

    public WorkspaceApplicationService(IWorkspaceRepository workspaceRepository)
    {
        _workspaceRepository = workspaceRepository;
    }

    public AnalysisWorkspace CreateWorkspace(string name) => AnalysisWorkspace.CreateNew(name);

    public Task<AnalysisWorkspace> OpenAsync(string path, CancellationToken cancellationToken = default) =>
        _workspaceRepository.LoadAsync(path, cancellationToken);

    public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
        _workspaceRepository.SaveAsync(path, workspace, cancellationToken);
}
