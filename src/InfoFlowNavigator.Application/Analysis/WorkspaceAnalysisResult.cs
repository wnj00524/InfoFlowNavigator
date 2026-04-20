namespace InfoFlowNavigator.Application.Analysis;

public sealed record WorkspaceAnalysisResult(
    int EntityCount,
    int RelationshipCount,
    int EventCount,
    int EvidenceCount,
    IReadOnlyList<EntityTypeCount> EntityCountByType,
    IReadOnlyList<OrphanEntityInsight> OrphanEntities,
    IReadOnlyList<ConnectedEntityInsight> TopConnectedEntities,
    IReadOnlyList<RelationshipConfidenceGap> RelationshipsMissingConfidence,
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

public sealed record EvidenceAnalysisSummary(
    int TotalCount,
    int WithCitationCount,
    int MissingCitationCount,
    int WithConfidenceCount,
    int MissingConfidenceCount);

public sealed record AnalysisFinding(string Title, string Detail);
