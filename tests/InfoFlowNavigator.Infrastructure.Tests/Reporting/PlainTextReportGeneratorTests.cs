using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.Analysis;
using InfoFlowNavigator.Infrastructure.Reporting;

namespace InfoFlowNavigator.Infrastructure.Tests.Reporting;

public sealed class PlainTextReportGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_IncludesEntitiesRelationshipsAndEvidenceDetails()
    {
        var generator = new PlainTextReportGenerator(new WorkspaceAnalysisService());
        var workspace = AnalysisWorkspace.CreateNew("Case Report");
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person"));
        workspace = workspace.AddEntity(Entity.Create("Contoso", "Organization"));
        workspace = workspace.AddRelationship(Relationship.Create(workspace.Entities[0].Id, workspace.Entities[1].Id, "employed_by"));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview Summary", "INT-001", "Analyst notes", 0.75));

        var artifact = await generator.GenerateAsync(workspace);

        Assert.Contains("Workspace: Case Report", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Findings:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Entity Count By Type:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Top Connected Entities:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Relationships Missing Confidence:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Evidence Summary:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Entities:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Relationships:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Evidence:", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Interview Summary", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Citation: INT-001", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Notes: Analyst notes", artifact.Content, StringComparison.Ordinal);
        Assert.Contains("Confidence: 0.75", artifact.Content, StringComparison.Ordinal);
    }
}
