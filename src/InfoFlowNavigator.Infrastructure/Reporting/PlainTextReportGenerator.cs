using System.Text;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Analysis;
using InfoFlowNavigator.Application.Reporting;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Infrastructure.Reporting;

public sealed class PlainTextReportGenerator : IReportGenerator
{
    private readonly IAnalysisService _analysisService;

    public PlainTextReportGenerator(IAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<ReportArtifact> GenerateAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var analysis = await _analysisService.SummarizeAsync(workspace, cancellationToken);

        var builder = new StringBuilder()
            .AppendLine($"Workspace Briefing: {workspace.Name}")
            .AppendLine()
            .AppendLine("Workspace Summary")
            .AppendLine($"- Created (UTC): {workspace.CreatedAtUtc:O}")
            .AppendLine($"- Updated (UTC): {workspace.UpdatedAtUtc:O}")
            .AppendLine($"- Entities: {analysis.EntityCount}")
            .AppendLine($"- Relationships: {analysis.RelationshipCount}")
            .AppendLine($"- Events: {analysis.EventCount}")
            .AppendLine($"- Event Participants: {analysis.EventParticipantCount}")
            .AppendLine($"- Claims: {analysis.ClaimCount}")
            .AppendLine($"- Hypotheses: {analysis.HypothesisCount}")
            .AppendLine($"- Evidence: {analysis.EvidenceCount}")
            .AppendLine($"- Evidence Assessments: {analysis.EvidenceLinkCount}");

        AppendLines(builder, "Key Entities",
            analysis.TopConnectedEntities.Select(item => $"{item.Name} ({item.EntityType}) has {item.Degree} relationship links.")
                .Concat(analysis.TopEventParticipants.Select(item => $"{item.Name} appears in {item.EventCount} recorded events."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6));

        AppendLines(builder, "Key Events",
            workspace.Events
                .OrderBy(@event => @event.OccurredAtUtc ?? DateTimeOffset.MaxValue)
                .ThenBy(@event => @event.Title, StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .Select(@event =>
                {
                    var participationGap = analysis.EventParticipationGaps.FirstOrDefault(item => item.EventId == @event.Id);
                    var supportGap = analysis.EventsWithoutSupportingEvidence.Any(item => item.EventId == @event.Id)
                        ? "support gap"
                        : "supported";
                    var participantNote = participationGap is null
                        ? "participant coverage present"
                        : participationGap.Detail;
                    return $"{FormatEventLabel(@event.Title, @event.OccurredAtUtc)} [{supportGap}] - {participantNote}";
                }));

        AppendLines(builder, "Active Hypotheses",
            analysis.HypothesisSummaries
                .Where(item => item.Status == Domain.Hypotheses.HypothesisStatus.Active)
                .Select(item => $"{item.Title} [{item.Posture}] support {item.SupportWeight:0.##} vs contradiction {item.ContradictionWeight:0.##}."));

        AppendLines(builder, "Key Claims",
            workspace.Claims
                .OrderByDescending(claim => claim.Status == Domain.Claims.ClaimStatus.Active)
                .ThenByDescending(claim => claim.Confidence ?? 0d)
                .ThenBy(claim => claim.Statement, StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .Select(claim =>
                {
                    var unsupported = analysis.UnsupportedClaims.Any(item => item.ClaimId == claim.Id);
                    var contradictory = analysis.ContradictoryClaims.FirstOrDefault(item => item.ClaimId == claim.Id);
                    var posture = contradictory is not null
                        ? $"contradicted by {contradictory.ContradictionCount}"
                        : unsupported
                            ? "unsupported"
                            : "supported/contextual";
                    return $"{claim.Statement} [{claim.Status}, {claim.ClaimType}] - {posture}.";
                }));

        AppendLines(builder, "Strongest Support",
            analysis.HypothesisSummaries
                .SelectMany(summary => summary.SupportingEvidence.Take(2).Select(item => $"{summary.Title}: {item.EvidenceTitle} ({item.Weight:0.##})"))
                .Concat(analysis.Findings
                    .Where(item => item.Category == FindingCategory.Participation || item.Category == FindingCategory.Workspace)
                    .Take(2)
                    .Select(item => $"{item.Title}: {item.Detail}"))
                .Take(6));

        AppendLines(builder, "Strongest Contradictions",
            analysis.ContradictoryClaims.Select(item => $"{item.Statement}: {item.ContradictionCount} contradictory assessments.")
                .Concat(analysis.HypothesisConflicts.Select(item => $"{item.Title}: {item.Detail}"))
                .Take(6));

        AppendLines(builder, "Collection Priorities / Next Steps",
            analysis.Findings
                .Where(item => item.Severity != FindingSeverity.Info || item.Category == FindingCategory.Collection)
                .Take(8)
                .Select(item => string.IsNullOrWhiteSpace(item.RecommendedAction)
                    ? $"{item.Title}: {item.Detail}"
                    : $"{item.Title}: {item.RecommendedAction}"));

        if (analysis.NetworkExportReadinessIssues.Count > 0)
        {
            AppendLines(builder, "Network Export Note",
                analysis.NetworkExportReadinessIssues.Select(item => $"{item.Title}: {item.Detail}"));
        }

        return new ReportArtifact(
            "workspace-briefing.txt",
            "text/plain",
            builder.ToString());
    }

    private static string FormatEventLabel(string title, DateTimeOffset? occurredAtUtc) =>
        occurredAtUtc is null
            ? title
            : $"{occurredAtUtc:yyyy-MM-dd} {title}";

    private static void AppendLines(StringBuilder builder, string title, IEnumerable<string> lines)
    {
        builder.AppendLine()
            .AppendLine(title);

        var entries = lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(8)
            .ToArray();

        if (entries.Length == 0)
        {
            builder.AppendLine("- None");
            return;
        }

        foreach (var line in entries)
        {
            builder.AppendLine($"- {line}");
        }
    }
}
