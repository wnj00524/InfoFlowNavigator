using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Workspaces;
using InfoFlowNavigator.Domain.EvidenceLinks;
using InfoFlowNavigator.Domain.Workspaces;

namespace InfoFlowNavigator.Application.Tests.Workspaces;

public sealed class WorkspaceApplicationServiceTests
{
    [Fact]
    public void CreateWorkspace_ReturnsNewWorkspaceWithRequestedName()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());

        var workspace = service.CreateWorkspace("Bootstrap Workspace");

        Assert.Equal("Bootstrap Workspace", workspace.Name);
    }

    [Fact]
    public void AddEntityRelationshipAndEvent_UpdateWorkspace()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");

        workspace = service.AddEntity(workspace, "Alice", "Person");
        workspace = service.AddEntity(workspace, "Contoso", "Organization");
        workspace = service.AddRelationship(workspace, workspace.Entities[0].Id, workspace.Entities[1].Id, "employed_by");
        workspace = service.AddEvent(workspace, "Employment confirmed", DateTimeOffset.Parse("2026-04-20T12:00:00Z"), "Interview", 0.8);
        workspace = service.UpdateEvent(workspace, workspace.Events[0].Id, "Employment confirmed v2", workspace.Events[0].OccurredAtUtc, "Follow-up", 0.9);

        Assert.Equal(2, workspace.Entities.Count);
        Assert.Single(workspace.Relationships);
        Assert.Single(workspace.Events);
        Assert.Equal("Employment confirmed v2", workspace.Events[0].Title);
    }

    [Fact]
    public void AddUpdateAndRemoveEvidence_ThroughApplicationService_UpdatesWorkspace()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");

        workspace = service.AddEvidence(workspace, "Interview Summary", "INT-001", "First pass", 0.6);
        workspace = service.UpdateEvidence(workspace, workspace.Evidence[0].Id, "Interview Summary v2", "INT-002", "Revised notes", 0.9);
        workspace = service.RemoveEvidence(workspace, workspace.Evidence[0].Id);

        Assert.Empty(workspace.Evidence);
    }

    [Fact]
    public void AddRemoveEvidenceLinkAndQueryHelpers_Work()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");
        workspace = service.AddEntity(workspace, "Alice", "Person");
        workspace = service.AddEvidence(workspace, "Interview Summary", "INT-001", "Notes", 0.6);

        workspace = service.AddEvidenceLink(
            workspace,
            workspace.Evidence[0].Id,
            EvidenceLinkTargetKind.Entity,
            workspace.Entities[0].Id,
            "supports",
            "Directly supports the entity",
            0.8);

        var linkedEvidence = service.GetLinkedEvidenceByTarget(workspace, EvidenceLinkTargetKind.Entity, workspace.Entities[0].Id);
        Assert.Single(linkedEvidence);
        Assert.Equal("Interview Summary", linkedEvidence[0].Title);

        workspace = service.RemoveEvidenceLink(workspace, workspace.EvidenceLinks[0].Id);
        Assert.Empty(workspace.EvidenceLinks);
    }

    [Fact]
    public void RemoveRelationship_ThroughApplicationService_RemovesRelationship()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");
        workspace = service.AddEntity(workspace, "Alice", "Person");
        workspace = service.AddEntity(workspace, "Contoso", "Organization");
        workspace = service.AddRelationship(workspace, workspace.Entities[0].Id, workspace.Entities[1].Id, "employed_by");

        var updated = service.RemoveRelationship(workspace, workspace.Relationships[0].Id);

        Assert.Empty(updated.Relationships);
    }

    private sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
    {
        public Task<AnalysisWorkspace> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(AnalysisWorkspace.CreateNew(path));

        public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
