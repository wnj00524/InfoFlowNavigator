using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;
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

    public AnalysisWorkspace AddEvent(
        AnalysisWorkspace workspace,
        string title,
        DateTimeOffset? occurredAtUtc = null,
        string? notes = null,
        double? confidence = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var @event = Event.Create(title, occurredAtUtc, notes, confidence);
        return workspace.AddEvent(@event);
    }

    public AnalysisWorkspace UpdateEvent(
        AnalysisWorkspace workspace,
        Guid eventId,
        string title,
        DateTimeOffset? occurredAtUtc = null,
        string? notes = null,
        double? confidence = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var existing = workspace.Events.FirstOrDefault(@event => @event.Id == eventId)
            ?? throw new InvalidOperationException($"Event '{eventId}' does not exist in the workspace.");

        return workspace.UpdateEvent(existing.Update(title, occurredAtUtc, notes, confidence, existing.Tags, existing.Metadata));
    }

    public AnalysisWorkspace RemoveEvent(AnalysisWorkspace workspace, Guid eventId)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return workspace.RemoveEvent(eventId);
    }

    public AnalysisWorkspace AddHypothesis(
        AnalysisWorkspace workspace,
        string title,
        string statement,
        HypothesisStatus status = HypothesisStatus.Draft,
        double? confidence = null,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var hypothesis = Hypothesis.Create(title, statement, status, confidence, notes);
        return workspace.AddHypothesis(hypothesis);
    }

    public AnalysisWorkspace UpdateHypothesis(
        AnalysisWorkspace workspace,
        Guid hypothesisId,
        string title,
        string statement,
        HypothesisStatus status,
        double? confidence = null,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var existing = workspace.Hypotheses.FirstOrDefault(hypothesis => hypothesis.Id == hypothesisId)
            ?? throw new InvalidOperationException($"Hypothesis '{hypothesisId}' does not exist in the workspace.");

        return workspace.UpdateHypothesis(existing.Update(title, statement, status, confidence, notes, existing.Tags, existing.Metadata));
    }

    public AnalysisWorkspace RemoveHypothesis(AnalysisWorkspace workspace, Guid hypothesisId)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return workspace.RemoveHypothesis(hypothesisId);
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

    public AnalysisWorkspace AddEvidenceAssessment(
        AnalysisWorkspace workspace,
        Guid evidenceId,
        EvidenceLinkTargetKind targetKind,
        Guid targetId,
        EvidenceRelationToTarget relationToTarget,
        EvidenceStrength strength = EvidenceStrength.Moderate,
        string? notes = null,
        double? confidence = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var evidenceAssessment = EvidenceLink.Create(evidenceId, targetKind, targetId, relationToTarget, strength, notes, confidence);
        return workspace.AddEvidenceLink(evidenceAssessment);
    }

    public AnalysisWorkspace AddHypothesisEvidenceLink(
        AnalysisWorkspace workspace,
        Guid evidenceId,
        Guid hypothesisId,
        EvidenceRelationToTarget relationToTarget,
        EvidenceStrength strength = EvidenceStrength.Moderate,
        string? notes = null,
        double? confidence = null) =>
        AddEvidenceAssessment(workspace, evidenceId, EvidenceLinkTargetKind.Hypothesis, hypothesisId, relationToTarget, strength, notes, confidence);

    public AnalysisWorkspace RemoveEvidenceAssessment(AnalysisWorkspace workspace, Guid evidenceAssessmentId)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return workspace.RemoveEvidenceLink(evidenceAssessmentId);
    }

    public AnalysisWorkspace AddEvidenceLink(
        AnalysisWorkspace workspace,
        Guid evidenceId,
        EvidenceLinkTargetKind targetKind,
        Guid targetId,
        string? role = null,
        string? notes = null,
        double? confidence = null) =>
        AddEvidenceAssessment(
            workspace,
            evidenceId,
            targetKind,
            targetId,
            InferRelationFromRole(role),
            EvidenceStrength.Moderate,
            notes,
            confidence);

    public AnalysisWorkspace RemoveEvidenceLink(AnalysisWorkspace workspace, Guid evidenceLinkId) =>
        RemoveEvidenceAssessment(workspace, evidenceLinkId);

    public IReadOnlyList<LinkedEvidenceSummary> GetLinkedEvidenceByTarget(
        AnalysisWorkspace workspace,
        EvidenceLinkTargetKind targetKind,
        Guid targetId)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return workspace.EvidenceLinks
            .Where(link => link.TargetKind == targetKind && link.TargetId == targetId)
            .Join(
                workspace.Evidence,
                link => link.EvidenceId,
                evidence => evidence.Id,
                (link, evidence) => new LinkedEvidenceSummary(
                    link.Id,
                    evidence.Id,
                    evidence.Title,
                    evidence.Citation,
                    link.TargetKind,
                    link.TargetId,
                    link.RelationToTarget,
                    link.Strength,
                    link.Notes,
                    link.Confidence))
            .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.EvidenceId)
            .ToArray();
    }

    public IReadOnlyList<LinkedEvidenceSummary> GetSupportingEvidenceByTarget(
        AnalysisWorkspace workspace,
        EvidenceLinkTargetKind targetKind,
        Guid targetId) =>
        GetLinkedEvidenceByTarget(workspace, targetKind, targetId)
            .Where(item => item.RelationToTarget == EvidenceRelationToTarget.Supports || item.RelationToTarget == EvidenceRelationToTarget.DerivedFrom)
            .ToArray();

    public IReadOnlyList<LinkedEvidenceSummary> GetContradictingEvidenceByTarget(
        AnalysisWorkspace workspace,
        EvidenceLinkTargetKind targetKind,
        Guid targetId) =>
        GetLinkedEvidenceByTarget(workspace, targetKind, targetId)
            .Where(item => item.RelationToTarget == EvidenceRelationToTarget.Contradicts)
            .ToArray();

    public HypothesisEvidenceSummary GetHypothesisEvidenceSummary(AnalysisWorkspace workspace, Guid hypothesisId)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var hypothesis = workspace.Hypotheses.FirstOrDefault(item => item.Id == hypothesisId)
            ?? throw new InvalidOperationException($"Hypothesis '{hypothesisId}' does not exist in the workspace.");

        var linkedEvidence = GetLinkedEvidenceByTarget(workspace, EvidenceLinkTargetKind.Hypothesis, hypothesisId);
        return new HypothesisEvidenceSummary(
            hypothesis.Id,
            hypothesis.Title,
            linkedEvidence.Where(item => item.RelationToTarget == EvidenceRelationToTarget.Supports || item.RelationToTarget == EvidenceRelationToTarget.DerivedFrom).ToArray(),
            linkedEvidence.Where(item => item.RelationToTarget == EvidenceRelationToTarget.Contradicts).ToArray());
    }

    public Task<AnalysisWorkspace> OpenAsync(string path, CancellationToken cancellationToken = default) =>
        _workspaceRepository.LoadAsync(path, cancellationToken);

    public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
        _workspaceRepository.SaveAsync(path, workspace, cancellationToken);

    private static EvidenceRelationToTarget InferRelationFromRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return EvidenceRelationToTarget.Contextual;
        }

        var normalized = role.Trim().ToLowerInvariant();
        if (normalized.Contains("contrad"))
        {
            return EvidenceRelationToTarget.Contradicts;
        }

        if (normalized.Contains("support") || normalized.Contains("document") || normalized.Contains("corrobor"))
        {
            return EvidenceRelationToTarget.Supports;
        }

        if (normalized.Contains("mention"))
        {
            return EvidenceRelationToTarget.Mentions;
        }

        if (normalized.Contains("derived"))
        {
            return EvidenceRelationToTarget.DerivedFrom;
        }

        return EvidenceRelationToTarget.Contextual;
    }
}
