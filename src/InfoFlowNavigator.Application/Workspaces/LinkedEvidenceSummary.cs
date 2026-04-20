using InfoFlowNavigator.Domain.EvidenceLinks;

namespace InfoFlowNavigator.Application.Workspaces;

public sealed record LinkedEvidenceSummary(
    Guid EvidenceLinkId,
    Guid EvidenceId,
    string Title,
    string? Citation,
    EvidenceLinkTargetKind TargetKind,
    Guid TargetId,
    string? Role,
    string? LinkNotes,
    double? LinkConfidence);
