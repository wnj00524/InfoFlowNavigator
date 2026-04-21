using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;

namespace InfoFlowNavigator.Application.Analysis;

public sealed record WorkspaceAnalysisResult(
    int EntityCount,
    int RelationshipCount,
    int EventCount,
    int EventParticipantCount,
    int ClaimCount,
    int HypothesisCount,
    int EvidenceCount,
    int EvidenceLinkCount,
    IReadOnlyList<EntityTypeCount> EntityCountByType,
    IReadOnlyList<OrphanEntityInsight> OrphanEntities,
    IReadOnlyList<ConnectedEntityInsight> TopConnectedEntities,
    IReadOnlyList<RelationshipConfidenceGap> RelationshipsMissingConfidence,
    IReadOnlyList<UnsupportedRelationshipInsight> RelationshipsWithoutSupportingEvidence,
    IReadOnlyList<UnsupportedEventInsight> EventsWithoutSupportingEvidence,
    IReadOnlyList<UnsupportedClaimInsight> UnsupportedClaims,
    IReadOnlyList<ContradictoryClaimInsight> ContradictoryClaims,
    IReadOnlyList<ClaimHypothesisImpactInsight> ClaimHypothesisImpacts,
    IReadOnlyList<ActivityWithoutEventInsight> EntitiesWithActivityButNoEvents,
    IReadOnlyList<ChronologyGapInsight> ChronologyGaps,
    IReadOnlyList<EntityEventParticipationInsight> TopEventParticipants,
    IReadOnlyList<EntityCoOccurrenceInsight> RepeatedCoOccurrences,
    IReadOnlyList<EventParticipationGapInsight> EventParticipationGaps,
    IReadOnlyList<NetworkExportReadinessInsight> NetworkExportReadinessIssues,
    IReadOnlyList<HypothesisAssessmentSummary> HypothesisSummaries,
    IReadOnlyList<UnresolvedHypothesisInsight> UnresolvedHypotheses,
    IReadOnlyList<HypothesisConflictInsight> HypothesisConflicts,
    IReadOnlyList<CollectionGuidanceInsight> CollectionGuidance,
    EvidenceAnalysisSummary EvidenceSummary,
    IReadOnlyList<AnalysisFinding> Findings);

public static class WorkspaceAnalysisResultFactory
{
    public static WorkspaceAnalysisResult Empty(
        int entityCount = 0,
        int relationshipCount = 0,
        int eventCount = 0,
        int eventParticipantCount = 0,
        int claimCount = 0,
        int hypothesisCount = 0,
        int evidenceCount = 0,
        int evidenceLinkCount = 0,
        IReadOnlyList<AnalysisFinding>? findings = null) =>
        new(
            entityCount,
            relationshipCount,
            eventCount,
            eventParticipantCount,
            claimCount,
            hypothesisCount,
            evidenceCount,
            evidenceLinkCount,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            new EvidenceAnalysisSummary(0, 0, 0, 0, 0),
            findings ?? []);
}

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

public sealed record UnsupportedClaimInsight(Guid ClaimId, string Statement, ClaimStatus Status, Guid? HypothesisId);

public sealed record ContradictoryClaimInsight(Guid ClaimId, string Statement, int ContradictionCount, int SupportCount, Guid? HypothesisId);

public sealed record ClaimHypothesisImpactInsight(Guid ClaimId, Guid HypothesisId, string ClaimStatement, string HypothesisTitle, ClaimStatus ClaimStatus);

public sealed record ActivityWithoutEventInsight(Guid EntityId, string Name, string EntityType, int Degree);

public sealed record ChronologyGapInsight(
    Guid EarlierEventId,
    string EarlierEventTitle,
    DateTimeOffset EarlierOccurredAtUtc,
    Guid LaterEventId,
    string LaterEventTitle,
    DateTimeOffset LaterOccurredAtUtc,
    int GapDays);

public sealed record EntityEventParticipationInsight(Guid EntityId, string Name, string EntityType, int EventCount);

public sealed record EntityCoOccurrenceInsight(Guid FirstEntityId, Guid SecondEntityId, string FirstEntityName, string SecondEntityName, int SharedEventCount);

public sealed record EventParticipationGapInsight(Guid EventId, string EventTitle, int ParticipantCount, string Detail);

public sealed record NetworkExportReadinessInsight(string Title, string Detail);

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

public enum FindingSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}

public enum FindingCategory
{
    Workspace = 1,
    SupportGap = 2,
    Contradiction = 3,
    Timeline = 4,
    Hypothesis = 5,
    Collection = 6,
    Participation = 7,
    NetworkExport = 8
}

public sealed record AnalysisFinding(
    string Title,
    string Detail,
    FindingSeverity Severity = FindingSeverity.Info,
    FindingCategory Category = FindingCategory.Workspace,
    string? TargetKind = null,
    Guid? TargetId = null,
    string? RecommendedAction = null)
{
    public int PriorityScore =>
        ((int)Severity * 100) +
        Category switch
        {
            FindingCategory.Contradiction => 60,
            FindingCategory.Hypothesis => 50,
            FindingCategory.SupportGap => 40,
            FindingCategory.Participation => 30,
            FindingCategory.Timeline => 20,
            FindingCategory.NetworkExport => 15,
            FindingCategory.Collection => 10,
            _ => 0
        };
}
