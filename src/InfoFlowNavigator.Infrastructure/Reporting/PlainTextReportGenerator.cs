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
            .AppendLine($"Entities: {workspace.Entities.Count}")
            .AppendLine($"Relationships: {workspace.Relationships.Count}")
            .AppendLine($"Events: {workspace.Events.Count}")
            .AppendLine($"Sources: {workspace.Sources.Count}");

        return Task.FromResult(new ReportArtifact(
            "workspace-summary.txt",
            "text/plain",
            builder.ToString()));
    }
}
