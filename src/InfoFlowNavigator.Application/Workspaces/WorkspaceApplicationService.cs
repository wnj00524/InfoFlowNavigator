using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
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

    public AnalysisWorkspace UpdateEntity(
        AnalysisWorkspace workspace,
        Guid entityId,
        string name,
        string entityType,
        string? notes = null,
        double? confidence = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var existing = workspace.Entities.FirstOrDefault(entity => entity.Id == entityId)
            ?? throw new InvalidOperationException($"Entity '{entityId}' does not exist in the workspace.");

        return workspace.UpdateEntity(existing.Update(name, entityType, notes, confidence, existing.Tags, existing.Metadata));
    }

    public AnalysisWorkspace RemoveEntity(AnalysisWorkspace workspace, Guid entityId)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return workspace.RemoveEntity(entityId);
    }

    public AnalysisWorkspace RemoveRelationship(AnalysisWorkspace workspace, Guid relationshipId)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return workspace.RemoveRelationship(relationshipId);
    }

    public AnalysisWorkspace AddEvidence(
        AnalysisWorkspace workspace,
        string title,
        string? citation = null,
        string? notes = null,
        double? confidence = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var evidence = WorkspaceEvidence.Create(title, citation, notes, confidence);
        return workspace.AddEvidence(evidence);
    }

    public AnalysisWorkspace UpdateEvidence(
        AnalysisWorkspace workspace,
        Guid evidenceId,
        string title,
        string? citation = null,
        string? notes = null,
        double? confidence = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var existing = workspace.Evidence.FirstOrDefault(evidence => evidence.Id == evidenceId)
            ?? throw new InvalidOperationException($"Evidence '{evidenceId}' does not exist in the workspace.");

        return workspace.UpdateEvidence(existing.Update(title, citation, notes, confidence, existing.Tags, existing.Metadata));
    }

    public AnalysisWorkspace RemoveEvidence(AnalysisWorkspace workspace, Guid evidenceId)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return workspace.RemoveEvidence(evidenceId);
    }

    public Task<AnalysisWorkspace> OpenAsync(string path, CancellationToken cancellationToken = default) =>
        _workspaceRepository.LoadAsync(path, cancellationToken);

    public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
        _workspaceRepository.SaveAsync(path, workspace, cancellationToken);
}
