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
            .AppendLine($"Evidence: {analysis.EvidenceCount}")
            .AppendLine()
            .AppendLine("Findings:");

        AppendFindings(builder, analysis.Findings);
        builder.AppendLine()
            .AppendLine("Entity Count By Type:");

        if (analysis.EntityCountByType.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var item in analysis.EntityCountByType)
            {
                builder.AppendLine($"- {item.EntityType}: {item.Count}");
            }
        }

        builder.AppendLine()
            .AppendLine("Orphan Entities:");

        if (analysis.OrphanEntities.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var orphan in analysis.OrphanEntities)
            {
                builder.AppendLine($"- {orphan.Name} [{orphan.EntityType}]");
            }
        }

        builder.AppendLine()
            .AppendLine("Top Connected Entities:");

        if (analysis.TopConnectedEntities.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var entity in analysis.TopConnectedEntities)
            {
                builder.AppendLine($"- {entity.Name} [{entity.EntityType}] degree {entity.Degree}");
            }
        }

        builder.AppendLine()
            .AppendLine("Relationships Missing Confidence:");

        if (analysis.RelationshipsMissingConfidence.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var relationship in analysis.RelationshipsMissingConfidence)
            {
                builder.AppendLine($"- {relationship.SourceEntityName} -> {relationship.RelationshipType} -> {relationship.TargetEntityName}");
            }
        }

        builder.AppendLine()
            .AppendLine("Evidence Summary:")
            .AppendLine($"- Total evidence items: {analysis.EvidenceSummary.TotalCount}")
            .AppendLine($"- With citations: {analysis.EvidenceSummary.WithCitationCount}")
            .AppendLine($"- Missing citations: {analysis.EvidenceSummary.MissingCitationCount}")
            .AppendLine($"- With confidence: {analysis.EvidenceSummary.WithConfidenceCount}")
            .AppendLine($"- Missing confidence: {analysis.EvidenceSummary.MissingConfidenceCount}")
            .AppendLine()
            .AppendLine("Entities:");

        if (workspace.Entities.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var entity in workspace.Entities)
            {
                builder.AppendLine($"- {entity.Name} [{entity.EntityType}]");
            }
        }

        builder
            .AppendLine()
            .AppendLine("Relationships:");

        if (workspace.Relationships.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var relationship in workspace.Relationships)
            {
                var source = workspace.Entities.FirstOrDefault(entity => entity.Id == relationship.SourceEntityId)?.Name ?? relationship.SourceEntityId.ToString();
                var target = workspace.Entities.FirstOrDefault(entity => entity.Id == relationship.TargetEntityId)?.Name ?? relationship.TargetEntityId.ToString();
                builder.AppendLine($"- {source} -> {relationship.RelationshipType} -> {target}");
            }
        }

        builder
            .AppendLine()
            .AppendLine("Evidence:");

        if (workspace.Evidence.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var evidence in workspace.Evidence)
            {
                builder.AppendLine($"- {evidence.Title}");

                if (!string.IsNullOrWhiteSpace(evidence.Citation))
                {
                    builder.AppendLine($"  Citation: {evidence.Citation}");
                }

                if (!string.IsNullOrWhiteSpace(evidence.Notes))
                {
                    builder.AppendLine($"  Notes: {evidence.Notes}");
                }

                if (evidence.Confidence is not null)
                {
                    builder.AppendLine($"  Confidence: {evidence.Confidence:0.###}");
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
