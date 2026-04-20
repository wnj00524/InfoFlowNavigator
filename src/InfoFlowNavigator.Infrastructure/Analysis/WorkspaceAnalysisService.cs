using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Infrastructure.Analysis;

public sealed class WorkspaceAnalysisService : IAnalysisService
{
    public Task<WorkspaceAnalysisResult> SummarizeAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var entityCountByType = workspace.Entities
            .GroupBy(entity => entity.EntityType, StringComparer.OrdinalIgnoreCase)
            .Select(group => new EntityTypeCount(
                group.OrderBy(entity => entity.EntityType, StringComparer.OrdinalIgnoreCase).First().EntityType,
                group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.EntityType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var entityDegrees = workspace.Entities
            .Select(entity => new ConnectedEntityInsight(
                entity.Id,
                entity.Name,
                entity.EntityType,
                workspace.Relationships.Count(relationship =>
                    relationship.SourceEntityId == entity.Id || relationship.TargetEntityId == entity.Id)))
            .ToArray();

        var orphanEntities = entityDegrees
            .Where(entity => entity.Degree == 0)
            .Select(entity => new OrphanEntityInsight(entity.EntityId, entity.Name, entity.EntityType))
            .OrderBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entity => entity.EntityId)
            .ToArray();

        var topConnectedEntities = entityDegrees
            .Where(entity => entity.Degree > 0)
            .OrderByDescending(entity => entity.Degree)
            .ThenBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entity => entity.EntityId)
            .Take(5)
            .ToArray();

        var entityNamesById = workspace.Entities.ToDictionary(entity => entity.Id, entity => entity.Name);

        var relationshipsMissingConfidence = workspace.Relationships
            .Where(relationship => relationship.Confidence is null)
            .Select(relationship => new RelationshipConfidenceGap(
                relationship.Id,
                relationship.SourceEntityId,
                relationship.TargetEntityId,
                entityNamesById.GetValueOrDefault(relationship.SourceEntityId, relationship.SourceEntityId.ToString()),
                entityNamesById.GetValueOrDefault(relationship.TargetEntityId, relationship.TargetEntityId.ToString()),
                relationship.RelationshipType))
            .OrderBy(relationship => relationship.SourceEntityName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(relationship => relationship.TargetEntityName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(relationship => relationship.RelationshipType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(relationship => relationship.RelationshipId)
            .ToArray();

        var evidenceSummary = new EvidenceAnalysisSummary(
            workspace.Evidence.Count,
            workspace.Evidence.Count(evidence => !string.IsNullOrWhiteSpace(evidence.Citation)),
            workspace.Evidence.Count(evidence => string.IsNullOrWhiteSpace(evidence.Citation)),
            workspace.Evidence.Count(evidence => evidence.Confidence is not null),
            workspace.Evidence.Count(evidence => evidence.Confidence is null));

        var findings = BuildFindings(entityCountByType, orphanEntities, topConnectedEntities, relationshipsMissingConfidence, evidenceSummary);

        return Task.FromResult(new WorkspaceAnalysisResult(
            workspace.Entities.Count,
            workspace.Relationships.Count,
            workspace.Events.Count,
            workspace.Evidence.Count,
            entityCountByType,
            orphanEntities,
            topConnectedEntities,
            relationshipsMissingConfidence,
            evidenceSummary,
            findings));
    }

    private static IReadOnlyList<AnalysisFinding> BuildFindings(
        IReadOnlyList<EntityTypeCount> entityCountByType,
        IReadOnlyList<OrphanEntityInsight> orphanEntities,
        IReadOnlyList<ConnectedEntityInsight> topConnectedEntities,
        IReadOnlyList<RelationshipConfidenceGap> relationshipsMissingConfidence,
        EvidenceAnalysisSummary evidenceSummary)
    {
        var findings = new List<AnalysisFinding>();
        var totalEntities = entityCountByType.Sum(item => item.Count);

        if (totalEntities == 0)
        {
            findings.Add(new AnalysisFinding("Entity coverage", "No entities have been added yet."));
        }
        else
        {
            var largestType = entityCountByType[0];
            findings.Add(new AnalysisFinding(
                "Entity coverage",
                $"Workspace contains {totalEntities} entities across {entityCountByType.Count} types. Largest group: {largestType.EntityType} ({largestType.Count})."));
        }

        if (orphanEntities.Count == 0)
        {
            findings.Add(new AnalysisFinding("Orphan entities", "Every entity participates in at least one relationship."));
        }
        else
        {
            findings.Add(new AnalysisFinding(
                "Orphan entities",
                $"{orphanEntities.Count} entities have no relationships: {PreviewList(orphanEntities.Select(entity => entity.Name))}."));
        }

        if (topConnectedEntities.Count == 0)
        {
            findings.Add(new AnalysisFinding("Connectivity", "No connected entities yet because the workspace has no relationships."));
        }
        else
        {
            findings.Add(new AnalysisFinding(
                "Connectivity",
                $"Most connected entities: {PreviewList(topConnectedEntities.Select(entity => $"{entity.Name} ({entity.Degree})"))}."));
        }

        if (relationshipsMissingConfidence.Count == 0)
        {
            findings.Add(new AnalysisFinding("Relationship confidence", "All relationships currently have confidence values."));
        }
        else
        {
            findings.Add(new AnalysisFinding(
                "Relationship confidence",
                $"{relationshipsMissingConfidence.Count} relationships are missing confidence values, including {PreviewList(relationshipsMissingConfidence.Select(relationship => $"{relationship.SourceEntityName} -> {relationship.RelationshipType} -> {relationship.TargetEntityName}"))}."));
        }

        if (evidenceSummary.TotalCount == 0)
        {
            findings.Add(new AnalysisFinding("Evidence coverage", "No evidence items have been added yet."));
        }
        else
        {
            findings.Add(new AnalysisFinding(
                "Evidence coverage",
                $"{evidenceSummary.TotalCount} evidence items are recorded; {evidenceSummary.WithCitationCount} have citations and {evidenceSummary.MissingConfidenceCount} are still missing confidence values."));
        }

        return findings;
    }

    private static string PreviewList(IEnumerable<string> items)
    {
        const int maxItems = 3;
        var orderedItems = items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(maxItems + 1)
            .ToArray();

        if (orderedItems.Length == 0)
        {
            return "none";
        }

        if (orderedItems.Length <= maxItems)
        {
            return string.Join(", ", orderedItems);
        }

        return $"{string.Join(", ", orderedItems.Take(maxItems))}, and more";
    }
}
