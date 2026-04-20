using System.Text;
using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Reporting;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Infrastructure.Reporting;

public sealed class PlainTextReportGenerator : IReportGenerator
{
    public Task<ReportArtifact> GenerateAsync(AnalysisWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder()
            .AppendLine($"Workspace: {workspace.Name}")
            .AppendLine($"Created (UTC): {workspace.CreatedAtUtc:O}")
            .AppendLine($"Updated (UTC): {workspace.UpdatedAtUtc:O}")
            .AppendLine($"Entities: {workspace.Entities.Count}")
            .AppendLine($"Relationships: {workspace.Relationships.Count}")
            .AppendLine($"Events: {workspace.Events.Count}")
            .AppendLine($"Evidence: {workspace.Evidence.Count}")
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

        return Task.FromResult(new ReportArtifact(
            "workspace-summary.txt",
            "text/plain",
            builder.ToString()));
    }
}
