using InfoFlowNavigator.Domain.EvidenceLinks;

namespace InfoFlowNavigator.Application.Workspaces;

public sealed record LinkedEvidenceSummary(
    Guid EvidenceLinkId,
    Guid EvidenceId,
    string Title,
    string? Citation,
    EvidenceLinkTargetKind TargetKind,
    Guid TargetId,
    EvidenceRelationToTarget RelationToTarget,
    EvidenceStrength Strength,
    string? LinkNotes,
    double? LinkConfidence);
