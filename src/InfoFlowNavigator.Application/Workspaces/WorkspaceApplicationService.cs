using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Domain.Entities;
using InfoFlowNavigator.Domain.Relationships;
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

    public AnalysisWorkspace RenameWorkspace(AnalysisWorkspace workspace, string name)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return workspace.Rename(name);
    }

    public AnalysisWorkspace AddEntity(
        AnalysisWorkspace workspace,
        string name,
        string entityType,
        string? notes = null,
        double? confidence = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var entity = Entity.Create(name, entityType, notes, confidence);
        return workspace.AddEntity(entity);
    }

    public AnalysisWorkspace AddRelationship(
        AnalysisWorkspace workspace,
        Guid sourceEntityId,
        Guid targetEntityId,
        string relationshipType,
        string? notes = null,
        double? confidence = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var relationship = Relationship.Create(sourceEntityId, targetEntityId, relationshipType, notes, confidence);
        return workspace.AddRelationship(relationship);
    }

    public Task<AnalysisWorkspace> OpenAsync(string path, CancellationToken cancellationToken = default) =>
        _workspaceRepository.LoadAsync(path, cancellationToken);

    public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
        _workspaceRepository.SaveAsync(path, workspace, cancellationToken);
}
