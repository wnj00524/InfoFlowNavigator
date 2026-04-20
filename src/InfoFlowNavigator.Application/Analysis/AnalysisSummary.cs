namespace InfoFlowNavigator.Application.Analysis;

public sealed record AnalysisSummary(
    int EntityCount,
    int RelationshipCount,
    int EventCount,
    int SourceCount);
