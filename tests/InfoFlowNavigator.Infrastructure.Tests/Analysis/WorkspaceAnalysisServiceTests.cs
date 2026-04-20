using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Events;
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
        workspace = workspace.AddEvent(Event.Create("Interview", DateTimeOffset.Parse("2026-01-01T10:00:00Z")));
        workspace = workspace.AddEvent(Event.Create("Follow-up", DateTimeOffset.Parse("2026-03-15T10:00:00Z")));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Interview Note", "INT-001", "Alice mentioned Contoso.", 0.8));
        workspace = workspace.AddEvidence(WorkspaceEvidence.Create("Photo", notes: "Warehouse exterior."));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Relationship, workspace.Relationships[0].Id, "supports"));
        workspace = workspace.AddEvidenceLink(EvidenceLink.Create(workspace.Evidence[0].Id, EvidenceLinkTargetKind.Event, workspace.Events[0].Id, "documents"));

        var analysis = await service.SummarizeAsync(workspace);

        Assert.Equal(4, analysis.EntityCount);
        Assert.Equal(3, analysis.RelationshipCount);
        Assert.Equal(2, analysis.EventCount);
        Assert.Equal(2, analysis.EvidenceCount);
        Assert.Equal(2, analysis.EvidenceLinkCount);
        Assert.Single(analysis.OrphanEntities);
        Assert.Equal("Warehouse", analysis.OrphanEntities[0].Name);
        Assert.Equal(2, analysis.RelationshipsMissingConfidence.Count);
        Assert.Equal(2, analysis.RelationshipsWithoutSupportingEvidence.Count);
        Assert.Single(analysis.EventsWithoutSupportingEvidence);
        Assert.Equal("Follow-up", analysis.EventsWithoutSupportingEvidence[0].Title);
        Assert.Empty(analysis.EntitiesWithActivityButNoEvents);
        Assert.Single(analysis.ChronologyGaps);
        Assert.Equal(73, analysis.ChronologyGaps[0].GapDays);
        Assert.Contains(analysis.Findings, finding => finding.Title == "Relationship support");
        Assert.Contains(analysis.Findings, finding => finding.Title == "Chronology gaps");
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
        Assert.Empty(analysis.RelationshipsWithoutSupportingEvidence);
        Assert.Empty(analysis.EventsWithoutSupportingEvidence);
        Assert.Empty(analysis.EntitiesWithActivityButNoEvents);
        Assert.Empty(analysis.ChronologyGaps);
        Assert.Equal(0, analysis.EvidenceSummary.TotalCount);
        Assert.Contains(analysis.Findings, finding => finding.Title == "Entity coverage" && finding.Detail.Contains("No entities", StringComparison.Ordinal));
        Assert.Contains(analysis.Findings, finding => finding.Title == "Evidence coverage" && finding.Detail.Contains("No evidence", StringComparison.Ordinal));
    }
}
