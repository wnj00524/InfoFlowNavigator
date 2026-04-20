using InfoFlowNavigator.Domain.Entities;
using WorkspaceEvidence = InfoFlowNavigator.Domain.Evidence.Evidence;
using InfoFlowNavigator.Domain.Relationships;
using InfoFlowNavigator.Domain.Workspaces;
using InfoFlowNavigator.Infrastructure.Persistence;

namespace InfoFlowNavigator.Infrastructure.Tests.Persistence;

public sealed class JsonWorkspaceRepositoryTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsWorkspaceIncludingEvidence()
    {
        var repository = new JsonWorkspaceRepository();
        var workspace = AnalysisWorkspace.CreateNew("Round Trip Workspace", "Investigation notes", ["priority", "external"]);
        workspace = workspace.AddEntity(Entity.Create("Alice", "Person", "Primary subject", 0.8));
        workspace = workspace.AddEntity(Entity.Create("Contoso", "Organization", "Employer", 0.7));
        workspace = workspace.AddRelationship(
            Relationship.Create(
                workspace.Entities[0].Id,
                workspace.Entities[1].Id,
                "employed_by",
                "Confirmed through interview",
                0.9));
        workspace = workspace.AddEvidence(
            WorkspaceEvidence.Create(
                "Interview Summary",
                "INT-001",
                "Alice confirmed her role at Contoso.",
                0.85));

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ifn.json");

        try
        {
            await repository.SaveAsync(path, workspace);
            var reloaded = await repository.LoadAsync(path);
            var json = await File.ReadAllTextAsync(path);

            Assert.Equal(workspace.Name, reloaded.Name);
            Assert.Equal(workspace.Notes, reloaded.Notes);
            Assert.Equal(workspace.Tags, reloaded.Tags);
            Assert.Equal(2, reloaded.Entities.Count);
            Assert.Single(reloaded.Relationships);
            Assert.Single(reloaded.Evidence);

            Assert.Equal("Alice", reloaded.Entities[0].Name);
            Assert.Equal("Contoso", reloaded.Entities[1].Name);
            Assert.Equal("employed_by", reloaded.Relationships[0].RelationshipType);
            Assert.Equal("Interview Summary", reloaded.Evidence[0].Title);
            Assert.Equal("INT-001", reloaded.Evidence[0].Citation);
            Assert.Equal("Alice confirmed her role at Contoso.", reloaded.Evidence[0].Notes);
            Assert.Equal(0.85, reloaded.Evidence[0].Confidence);
            Assert.Contains("\"schemaVersion\": 1", json, StringComparison.Ordinal);
            Assert.Contains("\"evidence\"", json, StringComparison.OrdinalIgnoreCase);
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
