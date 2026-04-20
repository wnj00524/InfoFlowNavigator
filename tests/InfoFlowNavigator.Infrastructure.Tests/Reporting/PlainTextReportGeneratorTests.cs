using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Events;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.Analysis;
using InfoFlowNavigator.Infrastructure.Reporting;

namespace InfoFlowNavigator.Infrastructure.Tests.Reporting;

public sealed class PlainTextReportGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_IncludesEntitiesRelationshipsEventsAndEvidenceLinkDetails()
    {
        var generator = new PlainTextReportGenerator(new WorkspaceAnalysisService());
        var workspace = AnalysisWorkspace.CreateNew("Case Report");
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person"));
        workspace = workspace.AddEntity(Entity.Create("Contoso", "Organization"));
        workspace = workspace.AddRelationship(Relationship.Create(workspace.Entities[0].Id, workspace.Entities[1].Id, "employed_by"));
        workspace = workspace.AddEvent(Event.Create("Interview", DateTimeOffset.Parse("2026-04-20T12:00:00Z"), "Interviewed Alice"));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview Summary", "INT-001", "Analyst notes", 0.75));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Event, workspace.Events[0].Id, "documents"));

        var artifact = await generator.GenerateAsync(workspace);

        Assert.Contains("Workspace: Case Report", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Findings:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Entity Count By Type:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Top Connected Entities:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Relationships Missing Confidence:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Relationships Without Supporting Evidence:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Events Without Supporting Evidence:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Chronology Gaps:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Evidence Summary:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Entities:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Relationships:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Events:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Evidence:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Evidence Links:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Interview Summary", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Interviewed Alice", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Citation: INT-001", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Notes: Analyst notes", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Confidence: 0.75", artifact.Content, StringComparison.Ordinal);
    }
}
