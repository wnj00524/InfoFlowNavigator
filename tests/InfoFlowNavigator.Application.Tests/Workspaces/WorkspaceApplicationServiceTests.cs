using InfoFlowNavigator.Application.Abstractions;
using InfoFlowNavigator.Application.Workspaces;
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
    public void AddEntityAndRelationship_UpdatesWorkspace()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");

        workspace = service.AddEntity(workspace, "Alice", "Person");
        workspace = service.AddEntity(workspace, "Contoso", "Organization");
        workspace = service.AddRelationship(
            workspace,
            workspace.Entities[0].Id,
            workspace.Entities[1].Id,
            "employed_by");

        Assert.Equal(2, workspace.Entities.Count);
        Assert.Single(workspace.Relationships);
    }

    [Fact]
    public void UpdateEntity_ThroughApplicationService_UpdatesWorkspace()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");
        workspace = service.AddEntity(workspace, "Alice", "Person", "Initial note", 0.2);

        var updated = service.UpdateEntity(workspace, workspace.Entities[0].Id, "Alice Smith", "Subject", "Updated note", 0.8);

        Assert.Equal("Alice Smith", updated.Entities[0].Name);
        Assert.Equal("Subject", updated.Entities[0].EntityType);
        Assert.Equal("Updated note", updated.Entities[0].Notes);
        Assert.Equal(0.8, updated.Entities[0].Confidence);
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

    [Fact]
    public void AddUpdateAndRemoveEvidence_ThroughApplicationService_UpdatesWorkspace()
    {
        var service = new WorkspaceApplicationService(new InMemoryWorkspaceRepository());
        var workspace = service.CreateWorkspace("Bootstrap Workspace");

        workspace = service.AddEvidence(workspace, "Interview Summary", "INT-001", "First pass", 0.6);
        Assert.Single(workspace.Evidence);

        workspace = service.UpdateEvidence(workspace, workspace.Evidence[0].Id, "Interview Summary v2", "INT-002", "Revised notes", 0.9);
        Assert.Equal("Interview Summary v2", workspace.Evidence[0].Title);
        Assert.Equal("INT-002", workspace.Evidence[0].Citation);

        workspace = service.RemoveEvidence(workspace, workspace.Evidence[0].Id);
        Assert.Empty(workspace.Evidence);
    }

    private sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
    {
        public Task<AnalysisWorkspace> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(AnalysisWorkspace.CreateNew(path));

        public Task SaveAsync(string path, AnalysisWorkspace workspace, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
