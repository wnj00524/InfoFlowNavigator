using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;

namespace InfoFlowNavigator.Application.Analysis;

public sealed record WorkspaceAnalysisResult(
    int EntityCount,
    int RelationshipCount,
    int EventCount,
    int HypothesisCount,
    int EvidenceCount,
    int EvidenceLinkCount,
    IReadOnlyList<EntityTypeCount> EntityCountByType,
    IReadOnlyList<OrphanEntityInsight> OrphanEntities,
    IReadOnlyList<ConnectedEntityInsight> TopConnectedEntities,
    IReadOnlyList<RelationshipConfidenceGap> RelationshipsMissingConfidence,
    IReadOnlyList<UnsupportedRelationshipInsight> RelationshipsWithoutSupportingEvidence,
    IReadOnlyList<UnsupportedEventInsight> EventsWithoutSupportingEvidence,
    IReadOnlyList<ActivityWithoutEventInsight> EntitiesWithActivityButNoEvents,
    IReadOnlyList<ChronologyGapInsight> ChronologyGaps,
    IReadOnlyList<HypothesisAssessmentSummary> HypothesisSummaries,
    IReadOnlyList<UnresolvedHypothesisInsight> UnresolvedHypotheses,
    IReadOnlyList<HypothesisConflictInsight> HypothesisConflicts,
    IReadOnlyList<CollectionGuidanceInsight> CollectionGuidance,
    EvidenceAnalysisSummary EvidenceSummary,
    IReadOnlyList<AnalysisFinding> Findings);

public sealed record EntityTypeCount(string EntityType, int Count);

public sealed record OrphanEntityInsight(Guid EntityId, string Name, string EntityType);

public sealed record ConnectedEntityInsight(Guid EntityId, string Name, string EntityType, int Degree);

public sealed record RelationshipConfidenceGap(
    Guid RelationshipId,
    Guid SourceEntityId,
    Guid TargetEntityId,
    string SourceEntityName,
    string TargetEntityName,
    string RelationshipType);

public sealed record UnsupportedRelationshipInsight(Guid RelationshipId, string Description);

public sealed record UnsupportedEventInsight(Guid EventId, string Title, DateTimeOffset? OccurredAtUtc);

public sealed record ActivityWithoutEventInsight(Guid EntityId, string Name, string EntityType, int Degree);

public sealed record ChronologyGapInsight(
    Guid EarlierEventId,
    string EarlierEventTitle,
    DateTimeOffset EarlierOccurredAtUtc,
    Guid LaterEventId,
    string LaterEventTitle,
    DateTimeOffset LaterOccurredAtUtc,
    int GapDays);

public sealed record HypothesisAssessmentSummary(
    Guid HypothesisId,
    string Title,
    HypothesisStatus Status,
    double? HypothesisConfidence,
    int SupportCount,
    int ContradictionCount,
    double SupportWeight,
    double ContradictionWeight,
    string Posture,
    string Explanation,
    IReadOnlyList<HypothesisEvidenceLine> SupportingEvidence,
    IReadOnlyList<HypothesisEvidenceLine> ContradictingEvidence);

public sealed record HypothesisEvidenceLine(
    Guid EvidenceAssessmentId,
    Guid EvidenceId,
    string EvidenceTitle,
    EvidenceRelationToTarget RelationToTarget,
    EvidenceStrength Strength,
    double Weight,
    string? Citation,
    string? Notes);

public sealed record UnresolvedHypothesisInsight(Guid HypothesisId, string Title, string Detail);

public sealed record HypothesisConflictInsight(Guid HypothesisId, string Title, string Detail);

public sealed record CollectionGuidanceInsight(Guid? HypothesisId, string Title, string Detail);

public sealed record EvidenceAnalysisSummary(
    int TotalCount,
    int WithCitationCount,
    int MissingCitationCount,
    int WithConfidenceCount,
    int MissingConfidenceCount);

public sealed record AnalysisFinding(string Title, string Detail);
