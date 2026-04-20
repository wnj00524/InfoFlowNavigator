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
            .AppendLine($"Workspace: {workspace.Name}")
            .AppendLine($"Created (UTC): {workspace.CreatedAtUtc:O}")
            .AppendLine($"Updated (UTC): {workspace.UpdatedAtUtc:O}")
            .AppendLine($"Entities: {analysis.EntityCount}")
            .AppendLine($"Relationships: {analysis.RelationshipCount}")
            .AppendLine($"Events: {analysis.EventCount}")
            .AppendLine($"Hypotheses: {analysis.HypothesisCount}")
            .AppendLine($"Evidence: {analysis.EvidenceCount}")
            .AppendLine($"Evidence Assessments: {analysis.EvidenceLinkCount}")
            .AppendLine()
            .AppendLine("Findings:");

        AppendFindings(builder, analysis.Findings);
        builder.AppendLine().AppendLine("Hypothesis Inference:");

        if (analysis.HypothesisSummaries.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var summary in analysis.HypothesisSummaries)
            {
                builder.AppendLine($"- {summary.Title} [{summary.Posture}]");
                builder.AppendLine($"  Status: {summary.Status}");
                builder.AppendLine($"  Support: {summary.SupportCount} (weighted {summary.SupportWeight:0.##})");
                builder.AppendLine($"  Contradiction: {summary.ContradictionCount} (weighted {summary.ContradictionWeight:0.##})");
                builder.AppendLine($"  Explanation: {summary.Explanation}");
            }
        }

        builder.AppendLine().AppendLine("Collection Guidance:");

        if (analysis.CollectionGuidance.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var guidance in analysis.CollectionGuidance)
            {
                builder.AppendLine($"- {guidance.Title}: {guidance.Detail}");
            }
        }

        builder.AppendLine().AppendLine("Hypotheses:");

        if (workspace.Hypotheses.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var hypothesis in workspace.Hypotheses.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"- {hypothesis.Title} [{hypothesis.Status}]");
                builder.AppendLine($"  Statement: {hypothesis.Statement}");

                if (!string.IsNullOrWhiteSpace(hypothesis.Notes))
                {
                    builder.AppendLine($"  Notes: {hypothesis.Notes}");
                }
            }
        }

        builder.AppendLine().AppendLine("Evidence Assessments:");

        if (workspace.EvidenceLinks.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var link in workspace.EvidenceLinks.OrderBy(link => link.TargetKind).ThenBy(link => link.TargetId).ThenBy(link => link.EvidenceId))
            {
                var evidenceTitle = workspace.Evidence.FirstOrDefault(evidence => evidence.Id == link.EvidenceId)?.Title ?? link.EvidenceId.ToString();
                builder.AppendLine($"- {link.TargetKind} {link.TargetId}: {evidenceTitle}");
                builder.AppendLine($"  Relation: {link.RelationToTarget}");
                builder.AppendLine($"  Strength: {link.Strength}");

                if (!string.IsNullOrWhiteSpace(link.Notes))
                {
                    builder.AppendLine($"  Notes: {link.Notes}");
                }

                if (link.Confidence is not null)
                {
                    builder.AppendLine($"  Confidence: {link.Confidence:0.###}");
                }
            }
        }

        return new ReportArtifact(
            "workspace-summary.txt",
            "text/plain",
            builder.ToString());
    }

    private static void AppendFindings(StringBuilder builder, IReadOnlyList<AnalysisFinding> findings)
    {
        if (findings.Count == 0)
        {
            builder.AppendLine("- No findings generated.");
            return;
        }

        foreach (var finding in findings)
        {
            builder.AppendLine($"- {finding.Title}: {finding.Detail}");
        }
    }
}
