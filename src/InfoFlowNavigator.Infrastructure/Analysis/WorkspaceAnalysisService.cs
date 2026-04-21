using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Domain.Claims;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Hypotheses;
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
        var hypothesisTitlesById = workspace.Hypotheses.ToDictionary(hypothesis => hypothesis.Id, hypothesis => hypothesis.Title);

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

        var unsupportedClaims = workspace.Claims
            .Where(claim => claim.Status != ClaimStatus.Refuted)
            .Where(claim => !linksByTarget.TryGetValue((EvidenceLinkTargetKind.Claim, claim.Id), out var links) || !links.Any(IsSupportingRelation))
            .OrderByDescending(claim => claim.Status == ClaimStatus.Active)
            .ThenBy(claim => claim.Statement, StringComparer.OrdinalIgnoreCase)
            .Select(claim => new UnsupportedClaimInsight(claim.Id, claim.Statement, claim.Status, claim.HypothesisId))
            .ToArray();

        var contradictoryClaims = workspace.Claims
            .Select(claim =>
            {
                linksByTarget.TryGetValue((EvidenceLinkTargetKind.Claim, claim.Id), out var links);
                links ??= [];
                var supportCount = links.Count(IsSupportingRelation);
                var contradictionCount = links.Count(link => link.RelationToTarget == EvidenceRelationToTarget.Contradicts);
                return new { Claim = claim, supportCount, contradictionCount };
            })
            .Where(item => item.contradictionCount > 0 && (item.supportCount > 0 || item.Claim.Status == ClaimStatus.Active))
            .OrderByDescending(item => item.contradictionCount)
            .ThenByDescending(item => item.supportCount)
            .ThenBy(item => item.Claim.Statement, StringComparer.OrdinalIgnoreCase)
            .Select(item => new ContradictoryClaimInsight(item.Claim.Id, item.Claim.Statement, item.contradictionCount, item.supportCount, item.Claim.HypothesisId))
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
                              || (summary.SupportWeight < 1.0 && summary.ContradictionWeight < 1.0 && summary.Status != HypothesisStatus.Resolved))
            .Select(summary => new UnresolvedHypothesisInsight(summary.HypothesisId, summary.Title, summary.Explanation))
            .ToArray();

        var hypothesisConflicts = hypothesisSummaries
            .Where(summary => summary.SupportWeight >= 1.8 && summary.ContradictionWeight >= 1.8)
            .Select(summary => new HypothesisConflictInsight(
                summary.HypothesisId,
                summary.Title,
                $"Strong support ({summary.SupportCount}) and strong contradiction ({summary.ContradictionCount}) are both present."))
            .ToArray();

        var activeHypothesisIds = workspace.Hypotheses
            .Where(hypothesis => hypothesis.Status == HypothesisStatus.Active)
            .Select(hypothesis => hypothesis.Id)
            .ToHashSet();

        var claimHypothesisImpacts = workspace.Claims
            .Where(claim => claim.HypothesisId is not null && activeHypothesisIds.Contains(claim.HypothesisId.Value) && claim.Status == ClaimStatus.Active)
            .OrderBy(claim => hypothesisTitlesById.GetValueOrDefault(claim.HypothesisId!.Value, string.Empty), StringComparer.OrdinalIgnoreCase)
            .ThenBy(claim => claim.Statement, StringComparer.OrdinalIgnoreCase)
            .Select(claim => new ClaimHypothesisImpactInsight(
                claim.Id,
                claim.HypothesisId!.Value,
                claim.Statement,
                hypothesisTitlesById.GetValueOrDefault(claim.HypothesisId.Value, claim.HypothesisId.Value.ToString()),
                claim.Status))
            .ToArray();

        var topEventParticipants = workspace.EventParticipants
            .GroupBy(participant => participant.EntityId)
            .Select(group =>
            {
                var entity = workspace.Entities.First(item => item.Id == group.Key);
                return new EntityEventParticipationInsight(entity.Id, entity.Name, entity.EntityType, group.Select(item => item.EventId).Distinct().Count());
            })
            .OrderByDescending(item => item.EventCount)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();

        var repeatedCoOccurrences = workspace.EventParticipants
            .GroupBy(participant => participant.EventId)
            .SelectMany(group =>
            {
                var distinctParticipants = group
                    .Select(participant => participant.EntityId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToArray();

                var pairs = new List<(Guid FirstEntityId, Guid SecondEntityId)>();
                for (var index = 0; index < distinctParticipants.Length; index++)
                {
                    for (var secondIndex = index + 1; secondIndex < distinctParticipants.Length; secondIndex++)
                    {
                        pairs.Add((distinctParticipants[index], distinctParticipants[secondIndex]));
                    }
                }

                return pairs;
            })
            .GroupBy(pair => pair)
            .Where(group => group.Count() >= 2)
            .Select(group => new EntityCoOccurrenceInsight(
                group.Key.FirstEntityId,
                group.Key.SecondEntityId,
                entityNamesById.GetValueOrDefault(group.Key.FirstEntityId, group.Key.FirstEntityId.ToString()),
                entityNamesById.GetValueOrDefault(group.Key.SecondEntityId, group.Key.SecondEntityId.ToString()),
                group.Count()))
            .OrderByDescending(item => item.SharedEventCount)
            .ThenBy(item => item.FirstEntityName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.SecondEntityName, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();

        var participantCountByEventId = workspace.EventParticipants
            .GroupBy(participant => participant.EventId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var eventParticipationGaps = workspace.Events
            .Select(@event =>
            {
                participantCountByEventId.TryGetValue(@event.Id, out var participants);
                participants ??= [];
                var detail = participants.Length switch
                {
                    0 => "No participants have been recorded for this event.",
                    1 => "Only one participant has been recorded for this event.",
                    _ when participants.All(participant => (participant.Confidence ?? 0.5) < 0.6) => "Participants exist, but all are low-confidence entries.",
                    _ => string.Empty
                };

                return new EventParticipationGapInsight(@event.Id, @event.Title, participants.Length, detail);
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Detail))
            .OrderByDescending(item => item.ParticipantCount == 0)
            .ThenBy(item => item.EventTitle, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var networkExportReadinessIssues = BuildNetworkExportReadinessIssues(workspace, repeatedCoOccurrences, unsupportedClaims, eventParticipationGaps);
        var collectionGuidance = BuildCollectionGuidance(unresolvedHypotheses, hypothesisConflicts, relationshipsWithoutSupportingEvidence, eventsWithoutSupportingEvidence, unsupportedClaims, eventParticipationGaps);

        var findings = BuildFindings(
            entityCountByType,
            orphanEntities,
            topConnectedEntities,
            relationshipsMissingConfidence,
            relationshipsWithoutSupportingEvidence,
            eventsWithoutSupportingEvidence,
            unsupportedClaims,
            contradictoryClaims,
            claimHypothesisImpacts,
            entitiesWithActivityButNoEvents,
            chronologyGaps,
            topEventParticipants,
            repeatedCoOccurrences,
            eventParticipationGaps,
            networkExportReadinessIssues,
            evidenceSummary,
            hypothesisSummaries,
            unresolvedHypotheses,
            hypothesisConflicts,
            collectionGuidance);

        return Task.FromResult(new WorkspaceAnalysisResult(
            workspace.Entities.Count,
            workspace.Relationships.Count,
            workspace.Events.Count,
            workspace.EventParticipants.Count,
            workspace.Claims.Count,
            workspace.Hypotheses.Count,
            workspace.Evidence.Count,
            workspace.EvidenceLinks.Count,
            entityCountByType,
            orphanEntities,
            topConnectedEntities,
            relationshipsMissingConfidence,
            relationshipsWithoutSupportingEvidence,
            eventsWithoutSupportingEvidence,
            unsupportedClaims,
            contradictoryClaims,
            claimHypothesisImpacts,
            entitiesWithActivityButNoEvents,
            chronologyGaps,
            topEventParticipants,
            repeatedCoOccurrences,
            eventParticipationGaps,
            networkExportReadinessIssues,
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

    private static IReadOnlyList<NetworkExportReadinessInsight> BuildNetworkExportReadinessIssues(
        AnalysisWorkspace workspace,
        IReadOnlyList<EntityCoOccurrenceInsight> repeatedCoOccurrences,
        IReadOnlyList<UnsupportedClaimInsight> unsupportedClaims,
        IReadOnlyList<EventParticipationGapInsight> eventParticipationGaps)
    {
        var issues = new List<NetworkExportReadinessInsight>();

        if (workspace.Entities.Count == 0 && workspace.Events.Count == 0)
        {
            issues.Add(new NetworkExportReadinessInsight("Export will be sparse", "Add entities or events before exporting so the resulting graph is navigable."));
        }

        if (workspace.Relationships.Count == 0 && workspace.EventParticipants.Count == 0)
        {
            issues.Add(new NetworkExportReadinessInsight("No connecting edges", "The network currently lacks relationships and event participation edges."));
        }

        if (unsupportedClaims.Count > 0)
        {
            issues.Add(new NetworkExportReadinessInsight("Claim support remains light", $"{unsupportedClaims.Count} claim nodes will export without supporting evidence links."));
        }

        if (eventParticipationGaps.Count > 0 && repeatedCoOccurrences.Count == 0)
        {
            issues.Add(new NetworkExportReadinessInsight("Event context is thin", "Co-occurrence exploration will be limited until more event participants are captured."));
        }

        return issues;
    }

    private static HypothesisEvidenceLine ToHypothesisEvidenceLine(
        EvidenceLink link,
        IReadOnlyDictionary<Guid, Domain.Evidence.Evidence> evidenceById)
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
        IReadOnlyList<UnsupportedEventInsight> unsupportedEvents,
        IReadOnlyList<UnsupportedClaimInsight> unsupportedClaims,
        IReadOnlyList<EventParticipationGapInsight> eventParticipationGaps)
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

        if (unsupportedClaims.Count > 0)
        {
            guidance.Add(new CollectionGuidanceInsight(
                unsupportedClaims[0].HypothesisId,
                "Strengthen key claims",
                $"Attach direct support to the highest-value unsupported claim first: {unsupportedClaims[0].Statement}."));
        }

        if (eventParticipationGaps.Count > 0)
        {
            guidance.Add(new CollectionGuidanceInsight(
                null,
                "Improve event participant context",
                $"Expand participant coverage around {eventParticipationGaps[0].EventTitle} to strengthen co-occurrence and sequence analysis."));
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
        IReadOnlyList<UnsupportedClaimInsight> unsupportedClaims,
        IReadOnlyList<ContradictoryClaimInsight> contradictoryClaims,
        IReadOnlyList<ClaimHypothesisImpactInsight> claimHypothesisImpacts,
        IReadOnlyList<ActivityWithoutEventInsight> entitiesWithActivityButNoEvents,
        IReadOnlyList<ChronologyGapInsight> chronologyGaps,
        IReadOnlyList<EntityEventParticipationInsight> topEventParticipants,
        IReadOnlyList<EntityCoOccurrenceInsight> repeatedCoOccurrences,
        IReadOnlyList<EventParticipationGapInsight> eventParticipationGaps,
        IReadOnlyList<NetworkExportReadinessInsight> networkExportReadinessIssues,
        EvidenceAnalysisSummary evidenceSummary,
        IReadOnlyList<HypothesisAssessmentSummary> hypothesisSummaries,
        IReadOnlyList<UnresolvedHypothesisInsight> unresolvedHypotheses,
        IReadOnlyList<HypothesisConflictInsight> hypothesisConflicts,
        IReadOnlyList<CollectionGuidanceInsight> collectionGuidance)
    {
        var findings = new List<AnalysisFinding>();
        var totalEntities = entityCountByType.Sum(item => item.Count);

        findings.Add(totalEntities == 0
            ? new AnalysisFinding("Entity coverage", "No entities have been added yet.", FindingSeverity.Warning, FindingCategory.Workspace, null, null, "Add the key people, organizations, and assets first.")
            : new AnalysisFinding("Entity coverage", $"Workspace contains {totalEntities} entities across {entityCountByType.Count} types."));

        findings.Add(orphanEntities.Count == 0
            ? new AnalysisFinding("Orphan entities", "Every entity participates in at least one relationship.")
            : new AnalysisFinding(
                "Orphan entities",
                $"{orphanEntities.Count} entities have no relationships: {PreviewList(orphanEntities.Select(entity => entity.Name))}.",
                FindingSeverity.Warning,
                FindingCategory.SupportGap,
                "Entity",
                orphanEntities[0].EntityId,
                "Confirm whether these entities should be linked or deprioritized."));

        findings.Add(topConnectedEntities.Count == 0
            ? new AnalysisFinding("Connectivity", "No connected entities yet because the workspace has no relationships.")
            : new AnalysisFinding("Connectivity", $"Most connected entities: {PreviewList(topConnectedEntities.Select(entity => $"{entity.Name} ({entity.Degree})"))}."));

        findings.Add(relationshipsMissingConfidence.Count == 0
            ? new AnalysisFinding("Relationship confidence", "All relationships currently have confidence values.")
            : new AnalysisFinding(
                "Relationship confidence",
                $"{relationshipsMissingConfidence.Count} relationships are missing confidence values.",
                FindingSeverity.Info,
                FindingCategory.SupportGap,
                "Relationship",
                relationshipsMissingConfidence[0].RelationshipId,
                "Assign confidence values to the most operationally important relationships."));

        findings.Add(evidenceSummary.TotalCount == 0
            ? new AnalysisFinding("Evidence coverage", "No evidence items have been added yet.", FindingSeverity.Warning, FindingCategory.SupportGap, null, null, "Add at least one primary evidence item before advancing inference.")
            : new AnalysisFinding("Evidence coverage", $"{evidenceSummary.TotalCount} evidence items are recorded; {evidenceSummary.WithCitationCount} have citations."));

        findings.Add(relationshipsWithoutSupportingEvidence.Count == 0
            ? new AnalysisFinding("Relationship support", "Every relationship has at least one supporting evidence assessment.")
            : new AnalysisFinding(
                "Relationship support",
                $"{relationshipsWithoutSupportingEvidence.Count} relationships have no supporting evidence.",
                FindingSeverity.Warning,
                FindingCategory.SupportGap,
                "Relationship",
                relationshipsWithoutSupportingEvidence[0].RelationshipId,
                "Prioritize support for the most consequential relationships first."));

        findings.Add(eventsWithoutSupportingEvidence.Count == 0
            ? new AnalysisFinding("Event support", "Every event has at least one supporting evidence assessment.")
            : new AnalysisFinding(
                "Event support",
                $"{eventsWithoutSupportingEvidence.Count} events have no supporting evidence.",
                FindingSeverity.Warning,
                FindingCategory.SupportGap,
                "Event",
                eventsWithoutSupportingEvidence[0].EventId,
                "Attach corroborating evidence to core chronology anchors."));

        findings.Add(unsupportedClaims.Count == 0
            ? new AnalysisFinding("Claim support", "Every active claim has at least one supporting evidence assessment.")
            : new AnalysisFinding(
                "Claim support",
                $"{unsupportedClaims.Count} claims lack direct supporting evidence.",
                FindingSeverity.Warning,
                FindingCategory.SupportGap,
                "Claim",
                unsupportedClaims[0].ClaimId,
                "Support active claims before promoting them into briefings or downstream network analysis."));

        findings.Add(entitiesWithActivityButNoEvents.Count == 0
            ? new AnalysisFinding("Activity coverage", "Connected entities are matched by recorded events, or there is no connected activity yet.")
            : new AnalysisFinding(
                "Activity coverage",
                $"{entitiesWithActivityButNoEvents.Count} connected entities have activity in relationships but no events recorded yet.",
                FindingSeverity.Warning,
                FindingCategory.Participation,
                "Entity",
                entitiesWithActivityButNoEvents[0].EntityId,
                "Add the key events that explain the current relationship picture."));

        findings.Add(chronologyGaps.Count == 0
            ? new AnalysisFinding("Chronology gaps", "No large chronology gaps were detected among dated events.")
            : new AnalysisFinding(
                "Chronology gaps",
                $"Detected {chronologyGaps.Count} chronology gaps of 30 days or more.",
                FindingSeverity.Warning,
                FindingCategory.Timeline,
                "Event",
                chronologyGaps[0].EarlierEventId,
                "Investigate whether missing events or missing dates explain the gap."));

        findings.Add(topEventParticipants.Count == 0
            ? new AnalysisFinding("Event participation", "No event participants have been recorded yet.", FindingSeverity.Warning, FindingCategory.Participation, null, null, "Add participant rows to connect events back to entities.")
            : new AnalysisFinding("Event participation", $"Most active entities by event participation: {PreviewList(topEventParticipants.Select(item => $"{item.Name} ({item.EventCount})"))}."));

        if (repeatedCoOccurrences.Count > 0)
        {
            findings.Add(new AnalysisFinding(
                "Repeated co-occurrence",
                $"Repeated event pairings detected: {PreviewList(repeatedCoOccurrences.Select(item => $"{item.FirstEntityName} + {item.SecondEntityName} ({item.SharedEventCount})"))}.",
                FindingSeverity.Info,
                FindingCategory.Participation,
                "Entity",
                repeatedCoOccurrences[0].FirstEntityId,
                "Review the recurring pairings for possible operational clusters."));
        }

        foreach (var contradiction in contradictoryClaims.Take(3))
        {
            findings.Add(new AnalysisFinding(
                "Contradictory claim",
                $"{contradiction.Statement} has {contradiction.ContradictionCount} contradictory assessments against {contradiction.SupportCount} supporting assessments.",
                FindingSeverity.Critical,
                FindingCategory.Contradiction,
                "Claim",
                contradiction.ClaimId,
                "Resolve the contradiction before relying on the claim in active inference."));
        }

        foreach (var conflict in hypothesisConflicts.Take(3))
        {
            findings.Add(new AnalysisFinding(
                "Hypothesis conflict",
                conflict.Detail,
                FindingSeverity.Critical,
                FindingCategory.Contradiction,
                "Hypothesis",
                conflict.HypothesisId,
                "Prioritize discriminating evidence that breaks the tie."));
        }

        foreach (var impact in claimHypothesisImpacts.Take(3))
        {
            findings.Add(new AnalysisFinding(
                "Claim affecting active hypothesis",
                $"{impact.ClaimStatement} materially affects active hypothesis {impact.HypothesisTitle}.",
                FindingSeverity.Warning,
                FindingCategory.Hypothesis,
                "Claim",
                impact.ClaimId,
                "Review this claim alongside the active hypothesis posture."));
        }

        foreach (var unresolved in unresolvedHypotheses.Take(3))
        {
            findings.Add(new AnalysisFinding(
                "Unresolved hypothesis",
                $"{unresolved.Title}: {unresolved.Detail}",
                FindingSeverity.Warning,
                FindingCategory.Hypothesis,
                "Hypothesis",
                unresolved.HypothesisId,
                "Collect decisive support or contradiction."));
        }

        foreach (var gap in eventParticipationGaps.Take(3))
        {
            findings.Add(new AnalysisFinding(
                "Event participation gap",
                $"{gap.EventTitle}: {gap.Detail}",
                gap.ParticipantCount == 0 ? FindingSeverity.Warning : FindingSeverity.Info,
                FindingCategory.Participation,
                "Event",
                gap.EventId,
                "Add missing participants or raise confidence on existing rows."));
        }

        foreach (var readiness in networkExportReadinessIssues.Take(2))
        {
            findings.Add(new AnalysisFinding(
                "Network export readiness",
                readiness.Detail,
                FindingSeverity.Info,
                FindingCategory.NetworkExport,
                null,
                null,
                "Address this before sharing the exported graph broadly."));
        }

        foreach (var guidance in collectionGuidance.Take(3))
        {
            findings.Add(new AnalysisFinding(
                "Next step",
                $"{guidance.Title}: {guidance.Detail}",
                FindingSeverity.Info,
                FindingCategory.Collection,
                guidance.HypothesisId is null ? null : "Hypothesis",
                guidance.HypothesisId,
                guidance.Detail));
        }

        return findings
            .OrderByDescending(finding => finding.PriorityScore)
            .ThenBy(finding => finding.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.Detail, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
