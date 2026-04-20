using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Domain.EvidenceLinks;
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

        var linksByTarget = workspace.EvidenceLinks
            .GroupBy(link => (link.TargetKind, link.TargetId))
            .ToDictionary(group => group.Key, group => group.ToArray());

        var relationshipsWithoutSupportingEvidence = workspace.Relationships
            .Where(relationship => !linksByTarget.TryGetValue((EvidenceLinkTargetKind.Relationship, relationship.Id), out var links) ||
                                   !links.Any(IsSupportingRelation))
            .Select(relationship =>
            {
                var source = entityNamesById.GetValueOrDefault(relationship.SourceEntityId, relationship.SourceEntityId.ToString());
                var target = entityNamesById.GetValueOrDefault(relationship.TargetEntityId, relationship.TargetEntityId.ToString());
                return new UnsupportedRelationshipInsight(relationship.Id, $"{source} -> {relationship.RelationshipType} -> {target}");
            })
            .OrderBy(item => item.Description, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var eventsWithoutSupportingEvidence = workspace.Events
            .Where(@event => !linksByTarget.TryGetValue((EvidenceLinkTargetKind.Event, @event.Id), out var links) ||
                             !links.Any(IsSupportingRelation))
            .OrderBy(@event => @event.OccurredAtUtc ?? DateTimeOffset.MaxValue)
            .ThenBy(@event => @event.Title, StringComparer.OrdinalIgnoreCase)
            .Select(@event => new UnsupportedEventInsight(@event.Id, @event.Title, @event.OccurredAtUtc))
            .ToArray();

        var entitiesWithActivityButNoEvents = entityDegrees
            .Where(entity => entity.Degree > 0 && workspace.Events.Count == 0)
            .Select(entity => new ActivityWithoutEventInsight(entity.EntityId, entity.Name, entity.EntityType, entity.Degree))
            .OrderByDescending(entity => entity.Degree)
            .ThenBy(entity => entity.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var chronologyGaps = BuildChronologyGaps(workspace);
        var hypothesisSummaries = BuildHypothesisSummaries(workspace);
        var unresolvedHypotheses = hypothesisSummaries
            .Where(summary => summary.SupportCount == 0 && summary.ContradictionCount == 0
                              || (summary.SupportWeight < 1.0 && summary.ContradictionWeight < 1.0 && summary.Status != Domain.Hypotheses.HypothesisStatus.Resolved))
            .Select(summary => new UnresolvedHypothesisInsight(summary.HypothesisId, summary.Title, summary.Explanation))
            .ToArray();

        var hypothesisConflicts = hypothesisSummaries
            .Where(summary => summary.SupportWeight >= 1.8 && summary.ContradictionWeight >= 1.8)
            .Select(summary => new HypothesisConflictInsight(
                summary.HypothesisId,
                summary.Title,
                $"Strong support ({summary.SupportCount}) and strong contradiction ({summary.ContradictionCount}) are both present."))
            .ToArray();

        var collectionGuidance = BuildCollectionGuidance(unresolvedHypotheses, hypothesisConflicts, relationshipsWithoutSupportingEvidence, eventsWithoutSupportingEvidence);

        var findings = BuildFindings(
            entityCountByType,
            orphanEntities,
            topConnectedEntities,
            relationshipsMissingConfidence,
            relationshipsWithoutSupportingEvidence,
            eventsWithoutSupportingEvidence,
            entitiesWithActivityButNoEvents,
            chronologyGaps,
            evidenceSummary,
            hypothesisSummaries,
            unresolvedHypotheses,
            hypothesisConflicts,
            collectionGuidance);

        return Task.FromResult(new WorkspaceAnalysisResult(
            workspace.Entities.Count,
            workspace.Relationships.Count,
            workspace.Events.Count,
            workspace.Hypotheses.Count,
            workspace.Evidence.Count,
            workspace.EvidenceLinks.Count,
            entityCountByType,
            orphanEntities,
            topConnectedEntities,
            relationshipsMissingConfidence,
            relationshipsWithoutSupportingEvidence,
            eventsWithoutSupportingEvidence,
            entitiesWithActivityButNoEvents,
            chronologyGaps,
            hypothesisSummaries,
            unresolvedHypotheses,
            hypothesisConflicts,
            collectionGuidance,
            evidenceSummary,
            findings));
    }

    private static IReadOnlyList<ChronologyGapInsight> BuildChronologyGaps(AnalysisWorkspace workspace)
    {
        var datedEvents = workspace.Events
            .Where(@event => @event.OccurredAtUtc is not null)
            .OrderBy(@event => @event.OccurredAtUtc)
            .ThenBy(@event => @event.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var chronologyGaps = new List<ChronologyGapInsight>();
        for (var index = 1; index < datedEvents.Length; index++)
        {
            var earlier = datedEvents[index - 1];
            var later = datedEvents[index];
            var gapDays = (int)Math.Floor((later.OccurredAtUtc!.Value - earlier.OccurredAtUtc!.Value).TotalDays);

            if (gapDays >= 30)
            {
                chronologyGaps.Add(new ChronologyGapInsight(
                    earlier.Id,
                    earlier.Title,
                    earlier.OccurredAtUtc.Value,
                    later.Id,
                    later.Title,
                    later.OccurredAtUtc.Value,
                    gapDays));
            }
        }

        return chronologyGaps;
    }

    private static IReadOnlyList<HypothesisAssessmentSummary> BuildHypothesisSummaries(AnalysisWorkspace workspace)
    {
        var evidenceById = workspace.Evidence.ToDictionary(item => item.Id);

        return workspace.Hypotheses
            .OrderBy(hypothesis => hypothesis.Title, StringComparer.OrdinalIgnoreCase)
            .Select(hypothesis =>
            {
                var relevantLinks = workspace.EvidenceLinks
                    .Where(link => link.TargetKind == EvidenceLinkTargetKind.Hypothesis && link.TargetId == hypothesis.Id)
                    .ToArray();

                var supportingEvidence = relevantLinks
                    .Where(IsSupportingRelation)
                    .Select(link => ToHypothesisEvidenceLine(link, evidenceById))
                    .OrderByDescending(item => item.Weight)
                    .ThenBy(item => item.EvidenceTitle, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var contradictingEvidence = relevantLinks
                    .Where(link => link.RelationToTarget == EvidenceRelationToTarget.Contradicts)
                    .Select(link => ToHypothesisEvidenceLine(link, evidenceById))
                    .OrderByDescending(item => item.Weight)
                    .ThenBy(item => item.EvidenceTitle, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var supportWeight = supportingEvidence.Sum(item => item.Weight);
                var contradictionWeight = contradictingEvidence.Sum(item => item.Weight);
                var posture = DetermineHypothesisPosture(supportWeight, contradictionWeight, supportingEvidence.Length, contradictingEvidence.Length);
                var explanation = BuildHypothesisExplanation(posture, supportingEvidence.Length, contradictingEvidence.Length, supportWeight, contradictionWeight);

                return new HypothesisAssessmentSummary(
                    hypothesis.Id,
                    hypothesis.Title,
                    hypothesis.Status,
                    hypothesis.Confidence,
                    supportingEvidence.Length,
                    contradictingEvidence.Length,
                    supportWeight,
                    contradictionWeight,
                    posture,
                    explanation,
                    supportingEvidence,
                    contradictingEvidence);
            })
            .ToArray();
    }

    private static HypothesisEvidenceLine ToHypothesisEvidenceLine(EvidenceLink link, IReadOnlyDictionary<Guid, Domain.Evidence.Evidence> evidenceById)
    {
        var evidence = evidenceById[link.EvidenceId];
        return new HypothesisEvidenceLine(
            link.Id,
            evidence.Id,
            evidence.Title,
            link.RelationToTarget,
            link.Strength,
            CalculateAssessmentWeight(evidence.Confidence, link.Confidence, link.Strength),
            evidence.Citation,
            link.Notes);
    }

    private static string DetermineHypothesisPosture(double supportWeight, double contradictionWeight, int supportCount, int contradictionCount)
    {
        if (supportCount == 0 && contradictionCount == 0)
        {
            return "Unassessed";
        }

        if (supportWeight >= 1.8 && contradictionWeight >= 1.8)
        {
            return "Mixed";
        }

        if (supportWeight == 0 && contradictionWeight > 0)
        {
            return "Contradicted";
        }

        if (contradictionWeight == 0 && supportWeight >= 2.0)
        {
            return "Strong";
        }

        if (supportWeight > contradictionWeight * 1.5)
        {
            return "Supported";
        }

        if (contradictionWeight > supportWeight * 1.5)
        {
            return "Contradicted";
        }

        return supportWeight + contradictionWeight < 1.0 ? "Weak" : "Mixed";
    }

    private static string BuildHypothesisExplanation(string posture, int supportCount, int contradictionCount, double supportWeight, double contradictionWeight) =>
        posture switch
        {
            "Unassessed" => "No evidence assessments have been attached yet.",
            "Strong" => $"Support outweighs contradiction with {supportCount} supporting assessments and weighted support {supportWeight:0.##}.",
            "Supported" => $"Support currently leads with {supportCount} supporting assessments versus {contradictionCount} contradictory assessments.",
            "Contradicted" => $"Contradiction currently leads with weighted contradiction {contradictionWeight:0.##} against weighted support {supportWeight:0.##}.",
            "Mixed" => $"Support and contradiction are both material: support {supportWeight:0.##}, contradiction {contradictionWeight:0.##}.",
            _ => $"Evidence remains light: support {supportWeight:0.##}, contradiction {contradictionWeight:0.##}."
        };

    private static IReadOnlyList<CollectionGuidanceInsight> BuildCollectionGuidance(
        IReadOnlyList<UnresolvedHypothesisInsight> unresolvedHypotheses,
        IReadOnlyList<HypothesisConflictInsight> hypothesisConflicts,
        IReadOnlyList<UnsupportedRelationshipInsight> unsupportedRelationships,
        IReadOnlyList<UnsupportedEventInsight> unsupportedEvents)
    {
        var guidance = new List<CollectionGuidanceInsight>();

        foreach (var unresolved in unresolvedHypotheses.Take(3))
        {
            guidance.Add(new CollectionGuidanceInsight(
                unresolved.HypothesisId,
                $"Collect direct support for {unresolved.Title}",
                "Seek one strong corroborating source or one clear contradictory source to move this hypothesis out of the unresolved state."));
        }

        foreach (var conflict in hypothesisConflicts.Take(3))
        {
            guidance.Add(new CollectionGuidanceInsight(
                conflict.HypothesisId,
                $"Resolve conflict around {conflict.Title}",
                "Prioritize higher-confidence evidence that discriminates between the competing explanations."));
        }

        if (unsupportedRelationships.Count > 0)
        {
            guidance.Add(new CollectionGuidanceInsight(
                null,
                "Backfill unsupported relationships",
                $"Add supporting evidence to the weakest relationship claims first, beginning with {unsupportedRelationships[0].Description}."));
        }

        if (unsupportedEvents.Count > 0)
        {
            guidance.Add(new CollectionGuidanceInsight(
                null,
                "Backfill unsupported events",
                $"Attach corroborating evidence to underevidenced chronology entries, beginning with {unsupportedEvents[0].Title}."));
        }

        return guidance;
    }

    private static IReadOnlyList<AnalysisFinding> BuildFindings(
        IReadOnlyList<EntityTypeCount> entityCountByType,
        IReadOnlyList<OrphanEntityInsight> orphanEntities,
        IReadOnlyList<ConnectedEntityInsight> topConnectedEntities,
        IReadOnlyList<RelationshipConfidenceGap> relationshipsMissingConfidence,
        IReadOnlyList<UnsupportedRelationshipInsight> relationshipsWithoutSupportingEvidence,
        IReadOnlyList<UnsupportedEventInsight> eventsWithoutSupportingEvidence,
        IReadOnlyList<ActivityWithoutEventInsight> entitiesWithActivityButNoEvents,
        IReadOnlyList<ChronologyGapInsight> chronologyGaps,
        EvidenceAnalysisSummary evidenceSummary,
        IReadOnlyList<HypothesisAssessmentSummary> hypothesisSummaries,
        IReadOnlyList<UnresolvedHypothesisInsight> unresolvedHypotheses,
        IReadOnlyList<HypothesisConflictInsight> hypothesisConflicts,
        IReadOnlyList<CollectionGuidanceInsight> collectionGuidance)
    {
        var findings = new List<AnalysisFinding>();
        var totalEntities = entityCountByType.Sum(item => item.Count);

        findings.Add(totalEntities == 0
            ? new AnalysisFinding("Entity coverage", "No entities have been added yet.")
            : new AnalysisFinding("Entity coverage", $"Workspace contains {totalEntities} entities across {entityCountByType.Count} types."));

        findings.Add(orphanEntities.Count == 0
            ? new AnalysisFinding("Orphan entities", "Every entity participates in at least one relationship.")
            : new AnalysisFinding("Orphan entities", $"{orphanEntities.Count} entities have no relationships: {PreviewList(orphanEntities.Select(entity => entity.Name))}."));

        findings.Add(topConnectedEntities.Count == 0
            ? new AnalysisFinding("Connectivity", "No connected entities yet because the workspace has no relationships.")
            : new AnalysisFinding("Connectivity", $"Most connected entities: {PreviewList(topConnectedEntities.Select(entity => $"{entity.Name} ({entity.Degree})"))}."));

        findings.Add(relationshipsMissingConfidence.Count == 0
            ? new AnalysisFinding("Relationship confidence", "All relationships currently have confidence values.")
            : new AnalysisFinding("Relationship confidence", $"{relationshipsMissingConfidence.Count} relationships are missing confidence values."));

        findings.Add(evidenceSummary.TotalCount == 0
            ? new AnalysisFinding("Evidence coverage", "No evidence items have been added yet.")
            : new AnalysisFinding("Evidence coverage", $"{evidenceSummary.TotalCount} evidence items are recorded; {evidenceSummary.WithCitationCount} have citations."));

        findings.Add(relationshipsWithoutSupportingEvidence.Count == 0
            ? new AnalysisFinding("Relationship support", "Every relationship has at least one supporting evidence assessment.")
            : new AnalysisFinding("Relationship support", $"{relationshipsWithoutSupportingEvidence.Count} relationships have no supporting evidence."));

        findings.Add(eventsWithoutSupportingEvidence.Count == 0
            ? new AnalysisFinding("Event support", "Every event has at least one supporting evidence assessment.")
            : new AnalysisFinding("Event support", $"{eventsWithoutSupportingEvidence.Count} events have no supporting evidence."));

        findings.Add(entitiesWithActivityButNoEvents.Count == 0
            ? new AnalysisFinding("Activity coverage", "Connected entities are matched by recorded events, or there is no connected activity yet.")
            : new AnalysisFinding("Activity coverage", $"{entitiesWithActivityButNoEvents.Count} connected entities have activity in relationships but no events recorded yet."));

        findings.Add(chronologyGaps.Count == 0
            ? new AnalysisFinding("Chronology gaps", "No large chronology gaps were detected among dated events.")
            : new AnalysisFinding("Chronology gaps", $"Detected {chronologyGaps.Count} chronology gaps of 30 days or more."));

        if (hypothesisSummaries.Count == 0)
        {
            findings.Add(new AnalysisFinding("Hypotheses", "No hypotheses have been added yet."));
        }
        else
        {
            findings.Add(new AnalysisFinding("Hypotheses", $"{hypothesisSummaries.Count} hypotheses are being tracked. Current postures: {PreviewList(hypothesisSummaries.Select(item => $"{item.Title} ({item.Posture})"))}."));
        }

        foreach (var conflict in hypothesisConflicts.Take(3))
        {
            findings.Add(new AnalysisFinding("Hypothesis conflict", conflict.Detail));
        }

        foreach (var unresolved in unresolvedHypotheses.Take(3))
        {
            findings.Add(new AnalysisFinding("Unresolved hypothesis", $"{unresolved.Title}: {unresolved.Detail}"));
        }

        foreach (var guidance in collectionGuidance.Take(3))
        {
            findings.Add(new AnalysisFinding("Next step", $"{guidance.Title}: {guidance.Detail}"));
        }

        return findings;
    }

    private static bool IsSupportingRelation(EvidenceLink link) =>
        link.RelationToTarget == EvidenceRelationToTarget.Supports || link.RelationToTarget == EvidenceRelationToTarget.DerivedFrom;

    private static double CalculateAssessmentWeight(double? evidenceConfidence, double? assessmentConfidence, EvidenceStrength strength)
    {
        var evidenceFactor = evidenceConfidence ?? 0.5;
        var assessmentFactor = assessmentConfidence ?? 0.5;
        var strengthFactor = strength switch
        {
            EvidenceStrength.Weak => 0.6,
            EvidenceStrength.Moderate => 1.0,
            EvidenceStrength.Strong => 1.4,
            _ => 1.0
        };

        return evidenceFactor * assessmentFactor * strengthFactor * 2.0;
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
