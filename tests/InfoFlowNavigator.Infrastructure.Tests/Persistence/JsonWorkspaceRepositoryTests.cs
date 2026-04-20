using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.Persistence;

namespace InfoFlowNavigator.Infrastructure.Tests.Persistence;

public sealed class JsonWorkspaceRepositoryTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsWorkspace()
    {
        var repository = new JsonWorkspaceRepository();
        var workspace = AnalysisWorkspace.CreateNew("Round Trip Workspace");
        workspace = workspace.AddEntity(Domain.Entities.Entity.Create("Alice", "Person"));
        workspace = workspace.AddEntity(Domain.Entities.Entity.Create("Contoso", "Organization"));
        workspace = workspace.AddRelationship(
            Domain.Relationships.Relationship.Create(
                workspace.Entities[0].Id,
                workspace.Entities[1].Id,
                "employed_by"));

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ifn.json");

        try
        {
            await repository.SaveAsync(path, workspace);
            var reloaded = await repository.LoadAsync(path);
            var json = await File.ReadAllTextAsync(path);

            Assert.Equal(workspace.Name, reloaded.Name);
            Assert.Equal(workspace.Entities.Count, reloaded.Entities.Count);
            Assert.Equal(workspace.Relationships.Count, reloaded.Relationships.Count);
            Assert.Contains("\"schemaVersion\": 1", json, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
