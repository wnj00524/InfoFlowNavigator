namespace InfoFlowNavigator.Application.Workspaces;

public sealed record HypothesisEvidenceSummary(
    Guid HypothesisId,
    string Title,
    IReadOnlyList<LinkedEvidenceSummary> SupportingEvidence,
    IReadOnlyList<LinkedEvidenceSummary> ContradictingEvidence);
