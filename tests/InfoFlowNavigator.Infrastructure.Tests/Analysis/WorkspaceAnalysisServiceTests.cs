using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.Analysis;

namespace InfoFlowNavigator.Infrastructure.Tests.Analysis;

public sealed class WorkspaceAnalysisServiceTests
{
    [Fact]
    public async Task SummarizeAsync_ReturnsExplainableWorkspaceInsights()
    {
        var service = new WorkspaceAnalysisService();
        var workspace = AnalysisWorkspace.CreateNew("Case Alpha");
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person"));
        workspace = workspace.AddEntity(Entity.Create("Bob", "Person"));
        workspace = workspace.AddEntity(Entity.Create("Contoso", "Organization"));
        workspace = workspace.AddEntity(Entity.Create("Warehouse", "Location"));
        workspace = workspace.AddRelationship(Relationship.Create(workspace.Entities[0].Id, workspace.Entities[2].Id, "works_for"));
        workspace = workspace.AddRelationship(Relationship.Create(workspace.Entities[0].Id, workspace.Entities[1].Id, "associated_with", confidence: 0.7));
        workspace = workspace.AddRelationship(Relationship.Create(workspace.Entities[2].Id, workspace.Entities[1].Id, "employs"));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview", "INT-001", "Alice mentioned Contoso.", 0.8));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Photo", notes: "Warehouse exterior."));

        var analysis = await service.SummarizeAsync(workspace);

        Assert.Equal(4, analysis.EntityCount);
        Assert.Equal(3, analysis.RelationshipCount);
        Assert.Equal(2, analysis.EvidenceCount);

        Assert.Collection(
            analysis.EntityCountByType,
            item =>
            {
                Assert.Equal("Person", item.EntityType);
                Assert.Equal(2, item.Count);
            },
            item =>
            {
                Assert.Equal("Location", item.EntityType);
                Assert.Equal(1, item.Count);
            },
            item =>
            {
                Assert.Equal("Organization", item.EntityType);
                Assert.Equal(1, item.Count);
            });

        Assert.Single(analysis.OrphanEntities);
        Assert.Equal("Warehouse", analysis.OrphanEntities[0].Name);

        Assert.Collection(
            analysis.TopConnectedEntities,
            item =>
            {
                Assert.Equal("Alice", item.Name);
                Assert.Equal(2, item.Degree);
            },
            item =>
            {
                Assert.Equal("Bob", item.Name);
                Assert.Equal(2, item.Degree);
            },
            item =>
            {
                Assert.Equal("Contoso", item.Name);
                Assert.Equal(2, item.Degree);
            });

        Assert.Equal(2, analysis.RelationshipsMissingConfidence.Count);
        Assert.Equal(1, analysis.EvidenceSummary.WithCitationCount);
        Assert.Equal(1, analysis.EvidenceSummary.MissingCitationCount);
        Assert.Equal(1, analysis.EvidenceSummary.WithConfidenceCount);
        Assert.Equal(1, analysis.EvidenceSummary.MissingConfidenceCount);
        Assert.Contains(analysis.Findings, finding => finding.Title == "Orphan entities" && finding.Detail.Contains("Warehouse", StringComparison.Ordinal));
        Assert.Contains(analysis.Findings, finding => finding.Title == "Relationship confidence" && finding.Detail.Contains("missing confidence", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SummarizeAsync_EmptyWorkspaceReturnsDeterministicEmptyFindings()
    {
        var service = new WorkspaceAnalysisService();
        var workspace = AnalysisWorkspace.CreateNew("Empty Workspace");

        var analysis = await service.SummarizeAsync(workspace);

        Assert.Empty(analysis.EntityCountByType);
        Assert.Empty(analysis.OrphanEntities);
        Assert.Empty(analysis.TopConnectedEntities);
        Assert.Empty(analysis.RelationshipsMissingConfidence);
        Assert.Equal(0, analysis.EvidenceSummary.TotalCount);
        Assert.Contains(analysis.Findings, finding => finding.Title == "Entity coverage" && finding.Detail.Contains("No entities", StringComparison.Ordinal));
        Assert.Contains(analysis.Findings, finding => finding.Title == "Evidence coverage" && finding.Detail.Contains("No evidence", StringComparison.Ordinal));
    }
}
